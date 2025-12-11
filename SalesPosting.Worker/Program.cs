using Microsoft.EntityFrameworkCore;
using SalesPosting.Data.Data;
using SalesPosting.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        }
    ));

// Add configuration
builder.Services.Configure<ProcessingSettings>(
    builder.Configuration.GetSection("ProcessingSettings"));

// Add hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();