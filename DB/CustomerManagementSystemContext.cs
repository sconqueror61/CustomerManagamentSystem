using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CustomerManagementSystem.DB;

public partial class CustomerManagementSystemContext : DbContext
{
    public CustomerManagementSystemContext()
    {
    }

    public CustomerManagementSystemContext(DbContextOptions<CustomerManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<OrdersDetail> OrdersDetails { get; set; }

    public virtual DbSet<OrdersHistory> OrdersHistories { get; set; }

    public virtual DbSet<Pcategory> Pcategories { get; set; }

    public virtual DbSet<Pimage> Pimages { get; set; }

    public virtual DbSet<PmainCategory> PmainCategories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SessionDetail> SessionDetails { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBasket> UserBaskets { get; set; }

    public virtual DbSet<UserType> UserTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=SCONQUEROR61;Initial Catalog=CustomerManagementSystem;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CompanyName).HasMaxLength(50);
            entity.Property(e => e.ContactName).HasMaxLength(50);
            entity.Property(e => e.CreaterUserId).HasColumnName("createrUserID");
            entity.Property(e => e.Mail).HasMaxLength(50);
            entity.Property(e => e.RecordDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ServiceArea).HasMaxLength(50);
            entity.Property(e => e.Tel).HasMaxLength(15);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.RecordDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.ToTable("OrderStatus");

            entity.Property(e => e.OrderStatus1)
                .HasMaxLength(50)
                .HasColumnName("OrderStatus");
        });

        modelBuilder.Entity<OrdersDetail>(entity =>
        {
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.RecordDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<OrdersHistory>(entity =>
        {
            entity.ToTable("OrdersHistory");

            entity.Property(e => e.Date).HasColumnType("datetime");
        });

        modelBuilder.Entity<Pcategory>(entity =>
        {
            entity.ToTable("Pcategory");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CategoryDesc)
                .HasMaxLength(15)
                .HasColumnName("categoryDesc");
            entity.Property(e => e.CategoryId).HasColumnName("categoryId");
            entity.Property(e => e.CreaterUserId).HasColumnName("createrUserID");
        });

        modelBuilder.Entity<Pimage>(entity =>
        {
            entity.Property(e => e.CreaterUserId).HasColumnName("createrUserID");
            entity.Property(e => e.PictureUrl)
                .HasMaxLength(100)
                .IsFixedLength();
        });

        modelBuilder.Entity<PmainCategory>(entity =>
        {
            entity.Property(e => e.Categories).HasMaxLength(15);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Breakibility).HasColumnName("breakibility");
            entity.Property(e => e.CategoryId).HasColumnName("categoryId");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.CreaterUserId).HasColumnName("createrUserID");
            entity.Property(e => e.Description)
                .HasMaxLength(150)
                .HasColumnName("description");
            entity.Property(e => e.Explanation)
                .HasMaxLength(100)
                .IsFixedLength()
                .HasColumnName("explanation");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.Width).HasColumnName("width");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreaterUserId).HasColumnName("createrUserID");
            entity.Property(e => e.Description).HasMaxLength(50);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");

            entity.Property(e => e.Culture).HasMaxLength(140);
            entity.Property(e => e.EnterTime).HasColumnType("datetime");
            entity.Property(e => e.ExitTime).HasColumnType("datetime");
            entity.Property(e => e.Ipadress)
                .HasMaxLength(10)
                .HasColumnName("IPadress");
        });

        modelBuilder.Entity<SessionDetail>(entity =>
        {
            entity.ToTable("SessionDetail");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Path).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Adress).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(20);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Password)
                .HasMaxLength(20)
                .HasColumnName("password");
            entity.Property(e => e.SurName).HasMaxLength(50);
            entity.Property(e => e.UserTypeId).HasColumnName("userTypeId");
        });

        modelBuilder.Entity<UserBasket>(entity =>
        {
            entity.ToTable("UserBasket");

            entity.Property(e => e.RecordDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.Property(e => e.UserType1)
                .HasMaxLength(10)
                .HasColumnName("user_Type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
