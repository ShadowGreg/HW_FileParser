using HW_FileParser.Entities;
using Microsoft.EntityFrameworkCore;

namespace HW_FileParser.Data;
public class AppDataContext(DbContextOptions<AppDataContext> options): DbContext(options)
{
    public DbSet<DownloadData> DownloadDatas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<DownloadData>(entity =>
            {
                entity.ToTable("DownloadData");

                entity.HasIndex(e => e.Id, "IX_DownloadData_Id")
                      .IsUnique();
            });
    }
}