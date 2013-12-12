using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace WCFUtils
{
    
    /// <summary>
    /// Adapted from Darin Dimitrov http://stackoverflow.com/questions/3200197/creating-wcf-channelfactoryt/3201001#3201001
    /// </summary>
    public sealed class ChannelFactoryManager : IDisposable
    {
        private readonly Dictionary<Type, ChannelFactory> _factories = new Dictionary<Type, ChannelFactory>();
        private readonly object _syncRoot = new object();

        public T CreateChannel<T>() where T : class
        {
            return CreateChannel<T>("*", null);
        }

        public T CreateChannel<T>(string endpointConfigurationName) where T : class
        {
            return CreateChannel<T>(endpointConfigurationName, null);
        }

        public T CreateChannel<T>(string endpointConfigurationName, string endpointAddress) where T : class
        {
            T local = GetFactory<T>(endpointConfigurationName, endpointAddress).CreateChannel();
            ((IClientChannel)local).Faulted += ChannelFaulted;
            return local;
        }

        private ChannelFactory<T> GetFactory<T>(string endpointConfigurationName, string endpointAddress) where T : class
        {
            lock (_syncRoot)
            {
                ChannelFactory factory;
                if (!_factories.TryGetValue(typeof(T), out factory))
                {
                    factory = CreateFactoryInstance<T>(endpointConfigurationName, endpointAddress);
                    _factories.Add(typeof(T), factory);
                }
                return (factory as ChannelFactory<T>);
            }
        }

        private ChannelFactory CreateFactoryInstance<T>(string endpointConfigurationName, string endpointAddress)
        {
            ChannelFactory factory = string.IsNullOrEmpty(endpointAddress)
                ? new ChannelFactory<T>(endpointConfigurationName)
                : new ChannelFactory<T>(endpointConfigurationName, new EndpointAddress(endpointAddress));
            factory.Faulted += FactoryFaulted;
            factory.Open();
            return factory;
        }

        private void ChannelFaulted(object sender, EventArgs e)
        {
            var channel = (IClientChannel)sender;
            try
            {
                channel.Close();
            }
            catch
            {
                channel.Abort();
            }
            throw new ApplicationException("Exc_ChannelFailure");
        }

        private void FactoryFaulted(object sender, EventArgs args)
        {
            var factory = (ChannelFactory)sender;
            try
            {
                factory.Close();
            }
            catch
            {
                factory.Abort();
            }
            Type[] genericArguments = factory.GetType().GetGenericArguments();
            if (genericArguments.Length == 1)
            {
                Type key = genericArguments[0];
                if (_factories.ContainsKey(key))
                {
                    _factories.Remove(key);
                }
            }
            throw new ApplicationException("Exc_ChannelFactoryFailure");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            lock (_syncRoot)
            {
                foreach (Type type in _factories.Keys)
                {
                    ChannelFactory factory = _factories[type];
                    try
                    {
                        factory.Close();
                    }
                    catch
                    {
                        factory.Abort();
                    }
                }
                _factories.Clear();
            }
        }
    }
}
