var builder = DistributedApplication.CreateBuilder(args);

var root = Path.Join(builder.AppHostDirectory, "../..");

builder.AddDockerfile("wishlists", root)
	.WithExternalHttpEndpoints();

builder.Build().Run();
