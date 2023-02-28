using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;
namespace DIFixture.Test_classes;

[Register(ServiceLifetime.Singleton, typeof(IErrorLogger))]
internal sealed class ConsoleLoggerWithAttribute : IErrorLogger {}
