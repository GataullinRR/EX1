using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;

namespace DeviceBase.Devices
{
    public static class DevicesFactory
    {
        public static IRUSDevice InstantiateSpecificDevice(RUSDeviceId rusDevice, IRUSConnectionInterface connectionInterface)
        {
            return InstantiateSupported(connectionInterface).Single(d => d.Id == rusDevice);
        }

        public static IEnumerable<IRUSDevice> InstantiateSupported(IRUSConnectionInterface connectionInterface)
        {
            yield return new RUSShockSensor(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSIzmeritel(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSRotationSensor(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSInclinometr(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSTelemetry(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSTelesystem(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSLWDLink(instantiateConnectionInterface()).wrap(connectionInterface);
            yield return new RUSDriveControll(instantiateConnectionInterface()).wrap(connectionInterface);

            {
                var texModuleConnectionInterface = instantiateConnectionInterface()
                    .Register(new RetranslatingMiddleware(RUSDeviceId.RUS_TECHNOLOGICAL_MODULE), InterfaceMiddlewareOrder.WRAPPER);
                yield return new RUSTechnologicalModule(texModuleConnectionInterface,
                    new IRUSDevice[]
                    {
                        new RUSInclinometr(texModuleConnectionInterface).wrap(connectionInterface),
                        new RUSShockSensor(texModuleConnectionInterface).wrap(connectionInterface),
                        new RUSIzmeritel(texModuleConnectionInterface).wrap(connectionInterface),
                    }).wrap(connectionInterface);
            }

            {
                var texModuleConnectionInterface = instantiateConnectionInterface()
                    .Register(new RetranslatingMiddleware(RUSDeviceId.EMC_MODULE), InterfaceMiddlewareOrder.WRAPPER);
                yield return new RUSEMCModule(texModuleConnectionInterface,
                    new IRUSDevice[]
                    {
                        new RUSShockSensor(texModuleConnectionInterface).wrap(connectionInterface),
                    }).wrap(connectionInterface);
            }

            {
                var moduleConnectionInterface = instantiateConnectionInterface();
                yield return new RUSModule(moduleConnectionInterface,
                    new IRUSDevice[]
                    {
                        new RUSInclinometr(moduleConnectionInterface).wrap(connectionInterface),
                        new RUSShockSensor(moduleConnectionInterface).wrap(connectionInterface),
                        new RUSRotationSensor(moduleConnectionInterface).wrap(connectionInterface),
                    }).wrap(connectionInterface);
            }

            MiddlewaredConnectionInterfaceDecorator instantiateConnectionInterface()
            {
                var middlewared = new MiddlewaredConnectionInterfaceDecorator(connectionInterface);
                middlewared.Register(new FTDIToSalachovProtocolUnificationMiddleware(middlewared), InterfaceMiddlewareOrder.ADAPTER);

                return middlewared;
            }
        }

        static IRUSDevice wrap(this IRUSDevice baseDevice, IRUSConnectionInterface connectionInterface)
        {
            IRUSDevice device = new BusyWaitProxy(baseDevice);
            device = new CommandSupportedCheckingProxy(device);
            device = new StatusFeatureProviderProxy(device);
            device = new ErrorsCatchingProxy(device);
            device = new SynchronizationProxy(device, connectionInterface);
            device = new AsyncProxy(device);
            device = new NullArgumentToDefaultProxy(device);

            return device;
        }
    }
}
