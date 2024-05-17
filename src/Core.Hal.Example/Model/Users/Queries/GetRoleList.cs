using Core.Hal.Example.Model;

namespace Core.Hal.Example.Model.Users.Queries
{
    public class GetRoleList : IGetPagedItemsRequest
    {
        public string Name { get; set; }

        public int? Page { get; set; }

        public int? PageSize { get; set; }
    }
}