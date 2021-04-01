/*
 * Copyright 2021 VMware, Inc.
 * SPDX-License-Identifier: EPL-2.0
 */

package tests

import (
	"flag"
	"io"
	"math/rand"
	"net/http"
	"os"
	"path/filepath"
	"reflect"
	"testing"
	"time"

	"github.com/gavv/httpexpect/v2"
	"github.com/gin-gonic/gin"
	jsoniter "github.com/json-iterator/go"
	"github.com/jucardi/go-osx/paths"

	"sgtnserver/api"
	v2 "sgtnserver/api/v2"
	"sgtnserver/internal/logger"
)

var (
	json          = jsoniter.ConfigDefault
	log           = logger.Log.Sugar()
	GinTestEngine *gin.Engine
)

const BaseURL = v2.APIRoot
const (
	GetBundleURL              = BaseURL + "/translation/products/{productName}/versions/{version}/locales/{locale}/components/{component}"
	GetBundlesURL             = BaseURL + "/translation/products/{productName}/versions/{version}"
	PutBundlesURL             = BaseURL + "/translation/products/{productName}/versions/{version}"
	GetSupportedComponentsURL = BaseURL + "/translation/products/{productName}/versions/{version}/componentlist"
	GetSupportedLocalesURL    = BaseURL + "/translation/products/{productName}/versions/{version}/localelist"
	GetKeyURL                 = BaseURL + "/translation/products/{productName}/versions/{version}/locales/{locale}/components/{component}/keys/{key}"
	GetRegionsOfLanguagesURL  = BaseURL + "/locale/regionList"
)
const (
	GetCombinedURL              = BaseURL + "/combination/translationsAndPattern"
	GetCombinedByPostURL        = GetCombinedURL
	GetSupportedLanguageListURL = BaseURL + "/locale/supportedLanguageList"
)
const (
	GetPatternByLangRegURL = BaseURL + "/formatting/patterns"
	GetPatternByLocaleURL  = BaseURL + "/formatting/patterns/locales/{locale}"
)

const (
	Name, Version, Locale, Component = "VPE", "1.0.0", "en", "sunglow"
	Language, Region                 = "en", "US"
	Key, Msg                         = "message", "Message-en"
)

func TestMain(m *testing.M) {
	defer logger.Log.Sync()

	flag.Parse()

	GinTestEngine = api.InitServer()

	m.Run()
}

func init() {
	// Rid of debug output
	gin.SetMode(gin.TestMode)

	cwd, _ := os.Getwd()
	log.Infof("Current directory is: %s", cwd)

	wd := os.Getenv("WD")
	if len(wd) != 0 {
		log.Infof("WD environment variable is: %s", wd)
		os.Chdir(wd)
	} else {
		testDataPath := "testdata"
		if ok, err := paths.Exists(testDataPath); err != nil || !ok {
			logger.SLog.Debug("Project root isn't set. Please set WD environment variable to project root.\nTrying to find project root by testdata directory...")
			FindProjectRoot(testDataPath)
			cwd, _ = os.Getwd()
			log.Infof("Now current directory is: %s", cwd)
		}
	}

	log.Infof("CLI args are: %v", os.Args)
}

func CreateHTTPExpect(t *testing.T, ginEngine *gin.Engine) *httpexpect.Expect {
	// Create httpexpect instance
	return httpexpect.WithConfig(httpexpect.Config{
		Client: &http.Client{
			// Transport: httpexpect.NewBinder(etag.Handler(ginTestEngine, false)),
			Transport: httpexpect.NewBinder(ginEngine),
			Jar:       httpexpect.NewJar(),
		},
		Reporter: httpexpect.NewAssertReporter(t),
		Printers: []httpexpect.Printer{
			httpexpect.NewDebugPrinter(t, true),
		},
	})
}

func GetErrorAndData(r io.Reader) (bError *api.BusinessError, data interface{}) {
	body := new(api.Response)
	err := json.NewDecoder(r).Decode(body)
	if err != nil {
		log.Error(err.Error())
	}
	data = body.Data
	bError = body.Error
	return
}

// Returns an int >= min, < max
func RandomInt(min, max int) int {
	return min + rand.Intn(max-min)
}

// Generate a random string of a-z chars with len
func RandomString(len int) string {
	rand.Seed(time.Now().UnixNano())
	bytes := make([]byte, len)
	for i := 0; i < len; i++ {
		bytes[i] = byte(RandomInt(97, 122))
	}
	return string(bytes)
}

func FindProjectRoot(testDataPath string) {
	currentRoot, _ := os.Getwd()
	for {
		ok, err := paths.Exists(filepath.Join(currentRoot, testDataPath))
		if err == nil && ok {
			logger.SLog.Infof("Found project root: %s", currentRoot)
			os.Chdir(currentRoot)
			break
		}

		newRoot := filepath.Join(currentRoot, "..")
		if newRoot == currentRoot {
			logger.SLog.Debug("Failed to find project root")
			break
		}
		currentRoot = newRoot
	}
}

// Contain judge if a slice contains an element
func Contain(list interface{}, target interface{}) int {
	listKind := reflect.TypeOf(list).Kind()
	if listKind == reflect.Slice || listKind == reflect.Array {
		listValue := reflect.ValueOf(list)
		for i := 0; i < listValue.Len(); i++ {
			// XXX - panics if slice element points to an unexported struct field
			// see https://golang.org/pkg/reflect/#Value.Interface
			if target == listValue.Index(i).Interface() {
				return i
			}
		}
	}
	return -1
}
