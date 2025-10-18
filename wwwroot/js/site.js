<script>
    // 🛒 Hàm gọi API để lấy số lượng giỏ hàng
    function updateCartCount() {
        fetch('/Cart/GetCartCount')
            .then(res => res.json())
            .then(data => {
                const badge = document.getElementById('cart-count');
                if (badge) {
                    badge.textContent = data.count > 0 ? data.count : '';
                }
            })
            .catch(err => console.error('Lỗi load cart count:', err));
    }

    // 🕓 Cập nhật khi load trang
    document.addEventListener('DOMContentLoaded', updateCartCount);

    // 📦 Cho phép gọi lại thủ công khi thêm món
    window.refreshCartCount = updateCartCount;
</script>
