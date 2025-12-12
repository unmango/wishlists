using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var root = Path.GetFullPath("../..", builder.AppHostDirectory);

var nats = builder.AddNats("nats");

if (builder.Environment.IsDevelopment()) {
	var web = builder.AddBunApp("web", root, entryPoint: "aspire")
		.WithBunPackageInstallation()
		.WithHttpEndpoint(5173, env: "VITE_PORT", name: "http")
		.WithHttpHealthCheck("/", 200, "http");

	builder.AddGolangApp("api", root)
		.WaitFor(web)
		.WithReference(nats)
		.WithEnvironment("WISH_DEV", "true")
		.WithEnvironment("WISH_ASSETS", root)
		.WithEnvironment("VITE_URL", web.GetEndpoint("http")) // Fuckin love this
		.WithHttpEndpoint(8080, env: "WISH_PORT", name: "http")
		.WithHttpHealthCheck("/", 200, "http")
		.WithHttpHealthCheck("/healthz", 200)
		.WithExternalHttpEndpoints();
} else {
	builder.AddDockerfile("wishlists", root)
		.WithEndpoint(8080)
		.WithExternalHttpEndpoints();
}

builder.Build().Run();
