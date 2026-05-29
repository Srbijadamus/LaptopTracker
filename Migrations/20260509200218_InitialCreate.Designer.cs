using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LaptopTracker.Data;
#nullable disable
namespace LaptopTracker.Migrations
{
    [DbContext(typeof(LaptopTrackerDbContext))]
    [Migration("20260509200218_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
        }
    }
}
