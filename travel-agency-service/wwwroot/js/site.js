console.log("JS LOADED");

function initCarousel(wrapperId, leftArrowSelector, rightArrowSelector) {
    const wrapper = document.getElementById(wrapperId);
    const leftArrow = document.querySelector(leftArrowSelector);
    const rightArrow = document.querySelector(rightArrowSelector);

    if (!wrapper || !leftArrow || !rightArrow) return;

    const scrollAmount = 300;

    function updateArrows() {
        const maxScroll = wrapper.scrollWidth - wrapper.clientWidth;
        const scrollLeft = wrapper.scrollLeft;

        leftArrow.style.display = scrollLeft <= 5 ? "none" : "flex";
        rightArrow.style.display = scrollLeft >= maxScroll - 5 ? "none" : "flex";
    }

    // ❮ שמאלה = זז שמאלה
    leftArrow.addEventListener("click", () => {
        wrapper.scrollBy({ left: -scrollAmount, behavior: "smooth" });
    });

    // ❯ ימינה = זז ימינה
    rightArrow.addEventListener("click", () => {
        wrapper.scrollBy({ left: scrollAmount, behavior: "smooth" });
    });

    wrapper.addEventListener("scroll", updateArrows);
    updateArrows();
}

document.addEventListener("DOMContentLoaded", () => {
    initCarousel(
        "dealsWrapper",
        ".deals-arrow.left",   // ❮
        ".deals-arrow.right"   // ❯
    );

    initCarousel(
        "popularWrapper",
        ".popular-arrow.left",
        ".popular-arrow.right"
    );
});




const unitPrice = parseFloat(document.getElementById("unitPrice").innerText);

function calculateTotal() {
    const rooms = document.getElementById("rooms")?.value ?? 1;
    const total = unitPrice * rooms;
    document.getElementById("totalPrice").innerText = total.toFixed(2);
}

function renderChildrenAges() {
    const count = document.getElementById("childrenCount").value;
    const container = document.getElementById("childrenAgesContainer");
    container.innerHTML = "";

    for (let i = 0; i < count; i++) {
        container.innerHTML += `
                <div class="mb-2">
                    <label class="form-label">גיל ילד ${i + 1}</label>
                    <input type="number"
                           name="childrenAges"
                           class="form-control"
                           min="0"
                           required />
                </div>`;
    }
}

document.getElementById("rooms")?.addEventListener("change", calculateTotal);
calculateTotal();

let dragged;

document.querySelectorAll('.gallery-item').forEach(item => {
    item.addEventListener('dragstart', e => dragged = item);

    item.addEventListener('dragover', e => e.preventDefault());

    item.addEventListener('drop', e => {
        e.preventDefault();
        if (dragged !== item) {
            item.parentNode.insertBefore(dragged, item);
            updateOrder();
        }
    });
});

function updateOrder() {
    const order = [];
    document.querySelectorAll('.gallery-item').forEach(i => {
        order.push(i.dataset.image);
    });
    document.getElementById('GalleryOrder').value = order.join('\n');
}

updateOrder();

function openImageModal(src) {
    const modal = document.getElementById("imageModal");
    const img = document.getElementById("modalImage");

    img.src = src;
    modal.style.display = "flex";
}

function closeImageModal() {
    document.getElementById("imageModal").style.display = "none";
}

// סגירה עם ESC
document.addEventListener("keydown", e => {
    if (e.key === "Escape") closeImageModal();
});


function syncRooms(form) {
    const rooms = document.getElementById("roomsSelect").value;
    form.querySelector('input[name="rooms"]').value = rooms;

}


