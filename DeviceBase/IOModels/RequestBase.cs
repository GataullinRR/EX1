using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace DeviceBase.IOModels
{
    abstract class RequestBase<TResponse> : IRequest<TResponse>
        where TResponse : class, IResponse 
    {
        const int MAX_AMOUNT_OF_BYTES_INSIDE_LOG_ENTRY = 1000;

        public abstract RequestTimeout Timeout { get; }
        public abstract IEnumerable<byte> Serialized { get; }

        public abstract TResponse BuildErrorResponse(RequestStatus status);
        public async Task<TResponse> DeserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo)
        {
            var sw = Stopwatch.StartNew();
            await ThreadingUtils.ContinueAtThreadPull();

            var loggedResponseFuture = new LoggingResponseFutureDecorator(inputStream, MAX_AMOUNT_OF_BYTES_INSIDE_LOG_ENTRY);
            TResponse response = null;
            using (Logger.Indent)
            {
                Logger.LogInfo(null, "Чтение ответа...");
                try
                {
                    response = await deserializeResponseAsync(loggedResponseFuture, operationInfo);

                    logDataRead();
                }
                catch (TimeoutException ex)
                {
                    logError(ex);

                    response = BuildErrorResponse(RequestStatus.READ_TIMEOUT);
                }
                catch (Exception ex)
                {
                    logError(ex);

                    response = BuildErrorResponse(RequestStatus.DESERIALIZATION_ERROR);
                }
            }
            
            return response;

            void logDataRead()
            {
                Logger.LogInfo(null, $"Пакет ответа был успешно прочитан{Global.NL}Полная длина: {loggedResponseFuture.ReadCount}, длительность чтения: {sw.Elapsed.TotalMilliseconds.ToString("F2")} мс{getBufferRepresentation()}");
            }

            void logError(Exception ex)
            {
                Logger.LogError(null, $"Ошибка во время чтения/десериализации пакета. Было прочитано: {loggedResponseFuture.ReadCount}, длительность чтения: {sw.Elapsed.TotalMilliseconds.ToString("F2")} мс{getBufferRepresentation()}", ex);
            }

            string getBufferRepresentation()
            {
                var tooManyData = loggedResponseFuture.StorageCount < loggedResponseFuture.ReadCount;
                return $"{Global.NL}Первые {loggedResponseFuture.Capacity} байт из {loggedResponseFuture.ReadCount}".IfOrDefault(tooManyData) +
                       $"{Global.NL}Данные<HEX>:{loggedResponseFuture.Storage.Select(b => b.ToString("X2").PadLeft(3)).Aggregate(" ")}" +
                       $"{Global.NL}Данные<DEC>:{loggedResponseFuture.Storage.Select(b => b.ToString("D3").PadLeft(3)).Aggregate(" ")}";
            }
        }

        protected abstract Task<TResponse> deserializeResponseAsync(
            IResponseFuture inputStream,
            AsyncOperationInfo operationInfo);

        IResponse IRequest.BuildErrorResponse(RequestStatus status)
        {
            return BuildErrorResponse(status);
        }

        Task<IResponse> IRequest.DeserializeResponseAsync(IResponseFuture inputStream, AsyncOperationInfo operationInfo)
        {
            return DeserializeResponseAsync(inputStream, operationInfo).ThenDo(r => (IResponse)r);
        }
    }
}
