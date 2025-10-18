using System.ComponentModel.DataAnnotations;

namespace FoodOrderWeb.Models.ViewModel
{
    public class AddAddressViewModel
    {
        public string AddressId { get; set; }

        [StringLength(100)]
        public string Label { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        [StringLength(300)]
        public string Street { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string District { get; set; }

        public bool IsDefault { get; set; }
    }
}
