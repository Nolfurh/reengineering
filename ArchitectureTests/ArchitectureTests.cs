using NetArchTest.Rules;
using NUnit.Framework;

namespace ArchitectureTests;

public class ArchitectureTests
{

    private readonly System.Reflection.Assembly _clientAssembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;
    private readonly System.Reflection.Assembly _serverAssembly = typeof(EchoServer.Program).Assembly;
    
    [Test]
    public void Client_ShouldNot_DependOn_Server_Test()
    {
        var result = Types.InAssembly(_clientAssembly)
            .ShouldNot()
            .HaveDependencyOn("EchoServer")
            .GetResult();
        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Server_ShouldNot_DependOn_Client_Test()
    {
        var result = Types.InAssembly(_serverAssembly)
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientApp")
            .GetResult();
        Assert.That(result.IsSuccessful, Is.True);
    }
    
    [Test]
    public void Messages_ShouldNot_DependOn_Networking_Test()
    {
        var result = Types.InAssembly(_clientAssembly)
            .That()
            .ResideInNamespace("NetSdrClientApp.Messages")
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientApp.Networking")
            .GetResult();
        Assert.That(result.IsSuccessful, Is.True);
    }
    
    [Test]
    public void Types_ShouldNot_ResideIn_GlobalNamespace_Test()
    {
        var result = Types.InAssembly(_clientAssembly)
            .That()
            .DoNotHaveName("Program")
            .Should()
            .ResideInNamespace("NetSdrClientApp")
            .GetResult();
        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void EchoServer_ShouldNot_DependOn_NetSdrClientApp_Test()
    {
        var result = Types.InAssembly(_serverAssembly)
            .ShouldNot().HaveDependencyOn("NetSdrClientApp")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Only_NetSdrClient_Should_DependOn_Messages_Test()
    {
        var result = Types.InAssembly(_clientAssembly)
            .That().DoNotHaveName("NetSdrClient")
            .And().DoNotResideInNamespace("NetSdrClientApp.Messages")
            .ShouldNot().HaveDependencyOn("NetSdrClientApp.Messages")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void ClientCoreAndInfrastructure_ShouldNot_DependOn_Program_Test()
    {
        var result = Types.InAssembly(_clientAssembly)
            .That().DoNotHaveName("Program")
            .ShouldNot().HaveDependencyOn("Program")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void ServerCoreAndInfrastructure_ShouldNot_DependOn_Program_Test()
    {
        var result = Types.InAssembly(_serverAssembly)
            .That().DoNotHaveName("Program")
            .ShouldNot().HaveDependencyOn("Program")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }
}
