1)  add {ModelName}DbContext, contains DbSet<ModelName>add models
2)  register dbcontext in ConfigureServices(IServiceCollection services)
	services.AddDbContext<{ModelName}DbContext>
                (options => options.UseSqlServer(connection));

3) Nuget package manager:
	Add-Migration InitialCreate
	Update-Database
4) Scaffold model => Add > Controller -> MVC controllers with views, using Entity Framework - 
	-> Model class to ModelName and Data context class to {ModelName}DbContext.