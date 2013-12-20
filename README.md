WCFUtils
========
These classes can be used for generating WCF clients using IoC.

The ChannelFactoryManager should be configured as a singleton, and the WCFServiceInvoker as transient. E.g. using Unity:

    container.RegisterType<ChannelFactoryManager>(new ContainerControlledLifetimeManager());
    container.RegisterType<WCFServiceInvoker>(new TransientLifetimeManager());

The invoker can then be injected into classes and used as follows:

    int result = _invoker.InvokeService<ICalculatorService>(s => s.Add(2, 2));
    Assert.Equal(4, result);

