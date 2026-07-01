using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var dockerComposeEnvironment = builder.AddDockerComposeEnvironment("docker-compose")
    .WithDashboard(true);

var rabbitMqUsername = builder.AddParameter("rabbitmq-username", "admin");
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", "admin");

var messaging = builder.AddRabbitMQ("messaging", rabbitMqUsername, rabbitMqPassword)
    .WithDataBindMount("../../data/rabbitmq")
    .WithManagementPlugin()
    .WithComputeEnvironment(dockerComposeEnvironment);

var backend = builder.AddProject<Projects.Timesheetr_Api>("backend")
    .WithComputeEnvironment(dockerComposeEnvironment)
    .WithReference(messaging)
    .WaitFor(messaging);

if (builder.ExecutionContext.IsPublishMode)
{
    backend
        .WithEnvironment("DataPath", "/app/data")
        .WithAnnotation(new ContainerMountAnnotation(
            source: Path.GetFullPath("./data", builder.AppHostDirectory),
            target: "/app/data",
            type: ContainerMountType.BindMount,
            isReadOnly: false));
}

builder.AddViteApp("frontend", "../Timesheetr.WebApp")
    .WithComputeEnvironment(dockerComposeEnvironment)
    .WithEnvironment("services__messaging__management__0", messaging.GetEndpoint("management"))
    .WithEnvironment("VITE_RABBITMQ_USERNAME", rabbitMqUsername)
    .WithEnvironment("VITE_RABBITMQ_PASSWORD", rabbitMqPassword)
    .PublishAsStaticWebsite("/api", backend);

builder.Build().Run();
