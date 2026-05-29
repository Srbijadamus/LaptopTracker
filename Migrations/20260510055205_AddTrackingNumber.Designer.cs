using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LaptopTracker.Data;
#nullable disable
namespace LaptopTracker.Migrations
{
    [DbContext(typeof(LaptopTrackerDbContext))]
    [Migration("20260510055205_AddTrackingNumber")]
    partial class AddTrackingNumber
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
        }
    }
}
