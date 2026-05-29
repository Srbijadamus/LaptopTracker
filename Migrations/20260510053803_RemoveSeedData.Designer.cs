using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LaptopTracker.Data;
#nullable disable
namespace LaptopTracker.Migrations
{
    [DbContext(typeof(LaptopTrackerDbContext))]
    [Migration("20260510053803_RemoveSeedData")]
    partial class RemoveSeedData
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
        }
    }
}
