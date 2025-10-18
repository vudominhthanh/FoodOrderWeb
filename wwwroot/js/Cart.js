$(document).on('change', '.update-qty', function () {
    const id = $(this).data('id');
    const isCombo = $(this).data('combo');
    const size = $(this).data('size');
    const qty = $(this).val();

    $.post('/Cart/UpdateQuantity', { productId: id, isCombo: isCombo, menuItemSizeId: size, quantity: qty }, function () {
        location.reload();
    });
});

$(document).on('click', '.remove-item', function () {
    const id = $(this).data('id');
    const isCombo = $(this).data('combo');
    const size = $(this).data('size');

    $.post('/Cart/RemoveItem', { productId: id, isCombo: isCombo, menuItemSizeId: size }, function () {
        location.reload();
    });
});
