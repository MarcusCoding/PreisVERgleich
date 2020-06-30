namespace PreisVergleich.Models
{
    public class ProduktModell
    {
        public int produktID { get; set; }

        public int hardwareRatID { get; set; }

        public string articleName { get; set; }

        public string articlePicture { get; set; }

        public string hardwareRatURL { get; set; }

        public string compareURL { get; set; }

        public double hardwareRatPrice { get; set; }

        public double comparePrice { get; set; }

        public string State { get; set; }

        public double priceDifference { get; set; }

        public string compareSiteType { get; set; }
    }

    public enum OperationMode
    {
        CREATE = 0,
        UPDATE = 1,
    }
}
