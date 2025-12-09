var builder = DistributedApplication.CreateBuilder(args);

var root = Path.Join(builder.AppHostDirectory, "../..");

builder.AddDockerfile("wishlists", root)
	.WithEndpoint(8080, 8080, "http")
	.WithExternalHttpEndpoints();

builder.Build().Run();
