using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Utilities.Extensions;

namespace WidgetsCompositionRoot
{
    class DIContainerLoggingProxy : DIContainerProxyBase
    {
        public DIContainerLoggingProxy(IDIContainer @base) : base(@base)
        {

        }

        public override void Register<T>(T service, object scope = null)
        {
            Logger.LogInfo(null, $"Регистрация реализации сервиса: {typeof(T)} в контесте: {scope} реализация: {service}");

            try
            {
                base.Register(service, scope);

                Logger.LogOK(null, $"Сервис успешно зарегистрирован");
            }
            catch (Exception ex)
            {
                Logger.LogError(null, $"Ошибка регистрации сервиса", ex);

                throw;
            }
        }

        public override IEnumerable<T> ResolveAll<T>(object scope = null)
        {
            Logger.LogInfo(null, $"Получение сервисов: {typeof(T)} в контесте: {scope}");

            try
            {
                var result = base.ResolveAll<T>(scope);

                Logger.LogOK(null, $"Сервисы: {typeof(T)} найдены. Реализации:-NL{result.Select(r => r.ToString()).Aggregate("-NL")}");

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(null, $"Ошибка получения сервисов", ex);

                throw;
            }
        }

        public override T ResolveSingle<T>(object scope = null)
        {
            Logger.LogInfo(null, $"Получение сервиса: {typeof(T)} в контесте: {scope}");

            try
            {
                var result = base.ResolveSingle<T>(scope);

                Logger.LogOK(null, $"Сервис найден. Реализация: {result}");

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(null, $"Ошибка получения сервиса", ex);

                throw;
            }
        }

        public override IEnumerable<T> TryResolveAll<T>(object scope = null)
        {
            Logger.LogInfo(null, $"Получение сервисов: {typeof(T)} в контесте: {scope}");

            try
            {
                var result = base.TryResolveAll<T>(scope);

                if (result == null)
                {
                    Logger.LogWarning(null, $"Сервисы: {typeof(T)} не найдены в контексте: {scope}");
                }
                else
                {
                    Logger.LogOK(null, $"Сервисы найдены. Реализации:-NL{result.Select(r => r.ToString()).Aggregate("-NL")}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(null, $"Ошибка получения сервисов", ex);

                throw;
            }
        }

        public override T TryResolveSingle<T>(object scope = null)
        {
            Logger.LogInfo(null, $"Получение сервиса: {typeof(T)} в контесте: {scope}");

            try
            {
                var result = base.TryResolveSingle<T>(scope);

                if (result == null)
                {
                    Logger.LogWarning(null, $"Сервис: {typeof(T)} не найден в контексте: {scope}");
                }
                else
                {
                    Logger.LogOK(null, $"Сервис найден. Реализация: {result}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(null, $"Ошибка получения сервиса", ex);

                throw;
            }
        }
    }
}
