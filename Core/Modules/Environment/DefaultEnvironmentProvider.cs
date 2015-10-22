﻿using System;

namespace Nyan.Core.Modules.Environment
{
    public class DefaultEnvironmentProvider : IEnvironmentProvider
    {
        public IEnvironmentDescriptor Current
        {
            get { return DefaultEnvironmentDescriptor.Standard; }
            set { throw new NotImplementedException(); }
        }

        public string CurrentCode
        {
            get { return DefaultEnvironmentDescriptor.Standard.Code; }
        }

        public string Probe { get; private set; }

        public void ResetToDefault()
        {
            throw new NotImplementedException();
        }

        public IEnvironmentDescriptor Get(string serverName)
        {
            throw new NotImplementedException();
        }

        public event EventHandler EnvironmentChanged;
    }
}