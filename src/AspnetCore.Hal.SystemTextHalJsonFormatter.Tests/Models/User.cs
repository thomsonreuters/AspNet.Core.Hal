namespace AspnetCore.Hal.SystemTextHalJsonFormatter.Models
{

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class HalUser : User
    {
        public Link[] Links { get; set; }
    }


}
