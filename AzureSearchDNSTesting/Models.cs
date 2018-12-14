using System;
using System.Collections.Generic;
using System.Text;

namespace AzureSearchDNSTesting
{

    public class Rootobject
    {
        public Product[] Property1 { get; set; }
    }

    public class Product
    {
        public int searchscore { get; set; }
        public string ProductID { get; set; }
        public string Name { get; set; }
        public string ProductNumber { get; set; }
        public string Color { get; set; }
        public string StandardCost { get; set; }
        public string ListPrice { get; set; }
        public string Size { get; set; }
        public string Weight { get; set; }
        public int ProductCategoryID { get; set; }
        public int ProductModelID { get; set; }
        public DateTime SellStartDate { get; set; }
        public DateTime? SellEndDate { get; set; }
        public object DiscontinuedDate { get; set; }
        public string ThumbnailPhotoFileName { get; set; }
        public string rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

}
