using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Configuration;

namespace WCFUtils
{
    /// <summary>
    /// Adapted from Darin Dimitrov http://stackoverflow.com/questions/3200197/creating-wcf-channelfactoryt/3201001#3201001
    /// </summary>
    public class WCFServiceInvoker
    {
        private readonly ChannelFactoryManager _factoryManager;
        private static readonly ClientSection _clientSection = ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;

        public WCFServiceInvoker(ChannelFactoryManager factoryManager)
        {
            _factoryManager = factoryManager;
        }

        public TR InvokeService<T, TR>(Func<T, TR> invokeHandler) where T : class
        {
            var endpointNameAddressPair = GetEndpointNameAddressPair(typeof(T));
            var arg = _factoryManager.CreateChannel<T>(endpointNameAddressPair.Key, endpointNameAddressPair.Value);
            var communicationObject = (ICommunicationObject)arg;
            try
            {
                return invokeHandler(arg);
            }
            finally
            {
                DoTheShutdownDance(communicationObject);
            }
        }

        public void InvokeService<T>(Action<T> invokeHandler) where T : class
        {
            var endpointNameAddressPair = GetEndpointNameAddressPair(typeof(T));
            var arg = _factoryManager.CreateChannel<T>(endpointNameAddressPair.Key, endpointNameAddressPair.Value);
            var communicationObject = (ICommunicationObject)arg;
            try
            {
                invokeHandler(arg);
            }
            finally
            {
                DoTheShutdownDance(communicationObject);
            }
        }

        private static void DoTheShutdownDance(ICommunicationObject communicationObject)
        {
            try
            {
                if (communicationObject.State != CommunicationState.Faulted)
                {
                    communicationObject.Close();
                }
            }
            catch
            {
                communicationObject.Abort();
            }
        }

        private KeyValuePair<string, string> GetEndpointNameAddressPair(Type serviceContractType)
        {
            var configException = new ConfigurationErrorsException(string.Format("No client endpoint found for type {0}. Please add the section <client><endpoint name=\"myservice\" address=\"http://address/\" binding=\"basicHttpBinding\" contract=\"{0}\"/></client> in the config file.", serviceContractType));
            if (((_clientSection == null) || (_clientSection.Endpoints == null)) || (_clientSection.Endpoints.Count < 1))
            {
                throw configException;
            }
            foreach (ChannelEndpointElement element in _clientSection.Endpoints)
            {
                if (element.Contract == serviceContractType.ToString())
                {
                    return new KeyValuePair<string, string>(element.Name, element.Address.AbsoluteUri);
                }
            }
            throw configException;
        }
    }
}
