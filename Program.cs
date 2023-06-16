using ConsoleApp.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp
{
    public class Program
    {
        public static void Main()
        {
            using var context = new PruebaContext();
            // Recuperar datos de ventas de los últimos 30 días
            var salesData = context.Venta
                .Include(v => v.VentaDetalles)
                    .ThenInclude(vd => vd.IdProductoNavigation)
                        .ThenInclude(p => p.IdMarcaNavigation)
                .Include(v => v.IdLocalNavigation)
                .Where(v => v.Fecha >= DateTime.Now.AddDays(-30))
                .ToList();

            // Ventas totales en los últimos 30 días
            var totalSales = salesData.Sum(v => v.Total);
            var totalSalesCount = salesData.Count;
            Console.WriteLine($"Ventas totales en los últimos 30 días: {totalSales:C} ({totalSalesCount} ventas)");

            // Venta con el monto más alto
            var maxSale = salesData.OrderByDescending(v => v.Total).First();
            Console.WriteLine($"Venta con el monto más alto: {maxSale.Total:C} el {maxSale.Fecha}");

            // Producto con el monto total de ventas más alto
            var topProduct = salesData.SelectMany(v => v.VentaDetalles)
                .GroupBy(vd => vd.IdProductoNavigation.Nombre)
                .Select(g => new { Product = g.Key, TotalSales = g.Sum(vd => vd.TotalLinea) })
                .OrderByDescending(x => x.TotalSales)
                .First();
            Console.WriteLine($"Producto con el monto total de ventas más alto: {topProduct.Product} ({topProduct.TotalSales:C})");

            // Tienda con el monto de ventas más alto
            var topStore = salesData.GroupBy(v => v.IdLocalNavigation.Nombre)
                .Select(g => new { Store = g.Key, TotalSales = g.Sum(v => v.Total) })
                .OrderByDescending(x => x.TotalSales)
                .First();
            Console.WriteLine($"Tienda con el monto de ventas más alto: {topStore.Store} ({topStore.TotalSales:C})");

            // Marca con el margen de ganancias más alto
            var topBrand = salesData.SelectMany(v => v.VentaDetalles)
                .GroupBy(vd => vd.IdProductoNavigation.IdMarcaNavigation.Nombre)
                .Select(g => new { Brand = g.Key, ProfitMargin = g.Sum(vd => vd.Cantidad * (vd.PrecioUnitario - vd.IdProductoNavigation.CostoUnitario)) })
                .OrderByDescending(x => x.ProfitMargin)
                .First();
            Console.WriteLine($"Marca con el margen de ganancias más alto: {topBrand.Brand} ({topBrand.ProfitMargin:C})");

            // Producto más vendido en cada tienda
            var bestSellingProducts = salesData.GroupBy(v => v.IdLocalNavigation.Nombre)
                .Select(g => new
                {
                    Store = g.Key,
                    BestSellingProduct = g.SelectMany(v => v.VentaDetalles)
                        .GroupBy(vd => vd.IdProductoNavigation.Nombre)
                        .Select(pg => new { Product = pg.Key, QuantitySold = pg.Sum(vd => vd.Cantidad) })
                        .OrderByDescending(x => x.QuantitySold)
                        .First()
                });
            Console.WriteLine("Producto más vendido en cada tienda:");
            foreach (var item in bestSellingProducts)
            {
                Console.WriteLine($"- {item.Store}: {item.BestSellingProduct.Product} ({item.BestSellingProduct.QuantitySold} vendidos)");
            }
        }
    }
    public class VentasContext : DbContext
    {
        public DbSet<VentaDetalle> VentaDetalle { get; set; }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Venta> Venta { get; set; }
        public DbSet<Local> Local { get; set; }
        public DbSet<Marca> Marca { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Server=lab-defontana.caporvnn6sbh.us-east-1.rds.amazonaws.com,1433;Database=Prueba;User ID=ReadOnly;Password=d*3PSf2MmRX9vJtA5sgwSphCVQ26*T53uU;Encrypt=False;";
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    public class VentaDetalle
    {
        [Key]
        public long ID_VentaDetalle { get; set; }
        public long ID_Venta { get; set; }
        public int Precio_Unitario { get; set; }
        public int Cantidad { get; set; }
        public int TotalLinea { get; set; }
        public long ID_Producto { get; set; }

        [ForeignKey("ID_Venta")]
        public Venta ?Venta { get; set; }
        [ForeignKey("ID_Producto")]
        public Producto ?Producto { get; set; }
    }

    public class Producto
    {
        [Key]
        public long ID_Producto { get; set; }
        public string ?Nombre { get; set; }
        public string ?Codigo { get; set; }
        public long ID_Marca { get; set; }
        public string ?Modelo { get; set; }
        public int Costo_Unitario { get; set; }

        [ForeignKey("ID_Marca")]
        public Marca ?Marca { get; set; }
    }

    public class Venta
    {
        [Key]
        public long ID_Venta { get; set; }
        public int Total { get; set; }
        public DateTime Fecha { get; set; }
        public long ID_Local { get; set; }

        [ForeignKey("ID_Local")]
        public Local ?Local { get; set; }
    }

    public class Local
    {
        [Key]
        public long ID_Local { get; set; }
        public string ?Nombre { get; set; }
        public string ?Direccion { get; set; }
    }

    public class Marca
    {
        [Key]
        public long ID_Marca { get; set; }
        public string ?Nombre { get; set; }
    }
}

