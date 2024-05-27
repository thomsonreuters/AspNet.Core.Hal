namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter.Models
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
