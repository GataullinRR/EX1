using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.IOModels.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TinyConfig;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.Devices
{
    class BusyWaitProxy : RUSDeviceProxyBase
    {
        readonly static ConfigAccessor CONFIG = Configurable.CreateConfig("RUSDeviceBusyWaitProxy");
        readonly static ConfigProxy<int> PING_SEND_PERIOD = CONFIG.Read(1000);

        public BusyWaitProxy(IRUSDevice @base) : base(@base) { }

        public override async Task<BurnResult> BurnAsync(Command request, IEnumerable<IDataEntity> entities, DeviceOperationScope scope, AsyncOperationInfo cancellation)
        {
            var baseResult = await base.BurnAsync(request, entities, scope, cancellation);
            if (baseResult.Status == BurnStatus.OK && 
                request.GetInfo().WriteResponse == Attributes.WriteResponse.RICH)
            {
                var richResponse = new RichResponse(baseResult.ResponseSections[SalachovProtocol.BODY_SECTION_KEY]);
                if (richResponse.Type == RichResponseType.OPERATION_STATUS)
                {
                    if (richResponse.StatusResponseData != null)
                    {
                        switch (richResponse.StatusResponseData.Status)
                        {
                            case LastWriteRequestStatus.EXECUTED:
                                return baseResult;
                            case LastWriteRequestStatus.ERROR:
                                return new BurnResult(BurnStatus.ERROR_ON_DEVICE, baseResult.Response, baseResult.ResponseSections);
                            case LastWriteRequestStatus.PROCESSING:
                                {
                                    var result = await waitTillReadyAsync();
                                    return new BurnResult(result, baseResult.Response, baseResult.ResponseSections);
                                }
                            case LastWriteRequestStatus.BUSY:
                                {
                                    var result = await waitTillReadyAsync();
                                    if (result == BurnStatus.OK)
                                    {
                                        return await BurnAsync(request, entities, scope, cancellation);
                                    }
                                }
                                break;

                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            return baseResult;

            async Task<BurnStatus> waitTillReadyAsync()
            {
                var isCheckSupported = SupportedCommands.Contains(Command.PING);
                if (isCheckSupported)
                {
                    var period = new PeriodDelay(PING_SEND_PERIOD);
                    while (true)
                    {
                        await period.WaitTimeLeftAsync(cancellation);

                        var pingResult = await ReadAsync(Command.PING, scope, cancellation);
                        if (pingResult.Status == ReadStatus.OK)
                        {
                            var status = new RichResponse(pingResult.AnswerSections[SalachovProtocol.BODY_SECTION_KEY]).StatusResponseData;
                            if (status.Status.IsOneOf(LastWriteRequestStatus.BUSY, LastWriteRequestStatus.PROCESSING))
                            {
                                continue;
                            }
                            else if (status.Status == LastWriteRequestStatus.EXECUTED)
                            {
                                return BurnStatus.OK;
                            }
                            else
                            {
                                return BurnStatus.COULD_NOT_COMPLETE_WAIT_OPERATION;
                            }
                        }
                        else
                        {
                            return BurnStatus.COULD_NOT_COMPLETE_WAIT_OPERATION;
                        }
                    }
                }
                else
                {
                    return BurnStatus.COULD_NOT_COMPLETE_WAIT_OPERATION;
                }
            }
        }
    }
}
