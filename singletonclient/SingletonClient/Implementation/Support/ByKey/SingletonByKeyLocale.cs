﻿/*
 * Copyright 2020-2021 VMware, Inc.
 * SPDX-License-Identifier: EPL-2.0
 */

namespace SingletonClient.Implementation.Support.ByKey
{
    public interface ISingletonByKeyLocale
    {
        bool IsSource();
        bool IsSourceLocale();
        int GetKeyCountInComponent(int componentIndex);
        void CheckTask(int componentIndex, bool needCheck);
        bool GetMessage(int componentIndex, int pageIndex, int indexInPage, out string message, bool needCheck = true);
        bool SetMessage(string message, ISingletonComponent componentObject,
            int componentIndex, int pageIndex, int indexInPage);
    }

    public class SingletonByKeyLocale : ISingletonByKeyLocale
    {
        private readonly ISingletonByKey _bykey;
        private readonly bool _asSource;
        private readonly bool _isSourceLocale;

        // Data table
        private readonly SingletonByKeyTable<ISingletonComponent> _components;
        private readonly SingletonByKeyTable<string> _messages;

        public SingletonByKeyLocale(ISingletonByKey bykey, string locale, bool asSource)
        {
            this._bykey = bykey;
            ISingletonLocale singletonLocale = SingletonUtil.GetSingletonLocale(locale);
            this._asSource = asSource;
            string sourceLocale = _bykey.GetSourceLocale();
            this._isSourceLocale = singletonLocale.Contains(sourceLocale);

            _components = new SingletonByKeyTable<ISingletonComponent> (SingletonByKey.COMPONENT_PAGE_MAX_SIZE);
            _messages = new SingletonByKeyTable<string>(SingletonByKey.PAGE_MAX_SIZE);
        }

        /// <summary>
        /// ISingletonByKeyLocale
        /// </summary>
        public bool IsSource()
        {
            return _asSource;
        }

        /// <summary>
        /// ISingletonByKeyLocale
        /// </summary>
        public bool IsSourceLocale()
        {
            return _isSourceLocale;
        }

        /// <summary>
        /// ISingletonByKeyLocale
        /// </summary>
        public int GetKeyCountInComponent(int componentIndex)
        {
            int count = 0;
            for (int i = 0; i < SingletonByKey.PAGE_MAX_SIZE; i++)
            {
                string[] page = _messages.GetPage(i);
                if (page == null)
                {
                    continue;
                }
                for (int k = 0; k < page.Length; k++)
                {
                    if (page[k] == null)
                    {
                        continue;
                    }
                    SingletonByKeyItem item = _bykey.GetKeyItem(i, k);
                    if (item != null && item.ComponentIndex == componentIndex)
                    {
                        count++;
                    }
                }
            }
            return count;
        }


        public void CheckTask(int componentIndex, bool needCheck)
        {
            if (componentIndex >= 0 && needCheck)
            {
                ISingletonComponent componentObj = _components.GetItemByOneIndex(componentIndex);
                if (componentObj != null)
                {
                    ISingletonAccessTask task = componentObj.GetAccessTask();
                    if (task != null)
                    {
                        task.Check();
                    }
                }
            }
        }

        public bool GetMessage(int componentIndex, int pageIndex, int indexInPage, out string message, bool needCheck = true)
        {
            this.CheckTask(componentIndex, needCheck);

            message = _messages.GetItem(pageIndex, indexInPage);
            return message != null;
        }

        public bool SetMessage(string message, ISingletonComponent componentObject, 
            int componentIndex, int pageIndex, int indexInPage)
        {
            if (componentObject != null)
            {
                _components.SetItemByOneIndex(componentIndex, componentObject);
            }
            _messages.SetItem(pageIndex, indexInPage, message);
            return true;
        }
    }
}