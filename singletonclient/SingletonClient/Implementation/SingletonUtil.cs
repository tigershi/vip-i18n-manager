﻿/*
 * Copyright 2020 VMware, Inc.
 * SPDX-License-Identifier: EPL-2.0
 */

using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace SingletonClient.Implementation
{
    public class SingletonUtil
    {
        /// <summary>
        /// New a Hashtable object and make it thread safe.
        /// </summary>
        /// <returns></returns>
        public static Hashtable NewHashtable()
        {
            return Hashtable.Synchronized(new Hashtable());
        }

        /// <summary>
        /// Read resource bytes from assembly
        /// </summary>
        /// <param name="resourceBaseName"></param>
        /// <param name="assembly"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static Byte[] ReadResource(
            string resourceBaseName, Assembly assembly, string resourceName)
        {
            ResourceManager resourceManager = new ResourceManager(resourceBaseName, assembly);
            Byte[] bytes = (Byte[])resourceManager.GetObject(resourceName);
            return bytes;
        }

        /// <summary>
        /// Read a map from a resource by name and assembly.
        /// </summary>
        /// <param name="resourceBaseName"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Hashtable ReadResourceMap(string resourceBaseName, string locale, Assembly assembly)
        {
            Hashtable table = new Hashtable();

            ResourceManager resourceManager = new ResourceManager(resourceBaseName, assembly);
            string localeInUse = locale;
            if (resourceBaseName.EndsWith("_" + locale) || resourceBaseName.EndsWith("_" + NearLocale(locale)))
            {
                localeInUse = ConfigConst.DefaultLocale;
            }

            bool tryParents = SingletonUtil.NearLocale(ConfigConst.DefaultLocale).Equals(
                SingletonUtil.NearLocale(localeInUse));

            CultureInfo cultureInfo = new System.Globalization.CultureInfo(localeInUse);
            try
            {
                ResourceSet resourceSet = resourceManager.GetResourceSet(cultureInfo, true, tryParents);
                if (resourceSet != null)
                {
                    IDictionaryEnumerator enumerator = resourceSet.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        table[enumerator.Key] = enumerator.Value;
                    }
                    resourceSet.Close();
                }
            }
            catch (Exception e)
            {
            }
            return table;
        }

        /// <summary>
        /// Convert from UTF8 binary to a string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ConvertToText(Byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length > 2 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                bytes[0] = 0x01;
                bytes[1] = 0x01;
                bytes[2] = 0x01;
            }
            string text = System.Text.Encoding.UTF8.GetString(bytes);
            text = text.Replace("\u0001", "");
            return text;
        }

        public static JObject ConvertToDict(string text)
        {
            if (text == null)
            {
                return null;
            }
            try
            {
                JObject dict = JObject.Parse(text);
                return dict;
            } catch (Exception e)
            {
            }
            return null;
        }

        public static bool CheckResponseValid(JToken token, Hashtable headers)
        {
            if (headers != null)
            {
                if (SingletonConst.StatusNotModified.Equals(headers[SingletonConst.HeaderResponseCode]))
                {
                    return false;
                }
            }
            if (token == null)
            {
                return false;
            }
            JObject result = token.Value<JObject>(SingletonConst.KeyResult);
            JObject status = result.Value<JObject>(SingletonConst.KeyResponse);
            if (status != null)
            {
                int code = status.Value<int>(SingletonConst.KeyCode);
                if (code == 200 || code == 604) {
                    return true;
                }
            }
            return false;
        }

        public static JObject HttpGetJson(IAccessService accessService, string url, Hashtable headers)
        {
            JObject obj = new JObject();
            string text = accessService.HttpGet(url, headers);
            if (text != null)
            {
                JObject dict = ConvertToDict(text);
                obj.Add(SingletonConst.KeyResult, dict);
            }
            return obj;
        }

        public static JObject HttpPost(IAccessService accessService, string url, string text, Hashtable headers)
        {
            string responseData = accessService.HttpPost(url, text, headers);
            if (responseData == null)
            {
                return null;
            }

            JObject obj = new JObject();
            obj.Add(SingletonConst.KeyResult, ConvertToDict(responseData));
            return obj;
        }

        /// <summary>
        /// Get the root node of a yaml.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static YamlMappingNode GetYamlRoot(string text)
        {
            var input = new StringReader(text);
            var yaml = new YamlStream();
            yaml.Load(input);

            try
            {
                YamlMappingNode root = (YamlMappingNode)yaml.Documents[0].RootNode;
                return root;
            }
            catch (Exception e)
            {
            }

            return null;
        }

        public static string NearLocale(string locale)
        {
            return SingletonClientManager.GetInstance().GetFallbackLocale(locale);
        }

        public static string ReadTextFile(string path)
        {
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                return text;
            } catch (Exception e)
            {
            }
            return null;
        }
    }
}

