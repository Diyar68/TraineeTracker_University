// Joel Bitterlich 
// Skript für die Sortierung der Lektionen im Lehrplan. Nutzt die SortableJS Bibliothek.

(function () {
    'use strict';

    function init() {
        const lists = document.querySelectorAll('.lessons-list');
        if (!lists || lists.length === 0) return;

        lists.forEach(list => {
            // eslint-disable-next-line no-undef
            new Sortable(list, {
                animation: 150,
                handle: '.lesson-card',
                draggable: '.lesson-card',
                group: 'lessons',
                onEnd: sendOrder
            });
        });
    }

    function sendOrder() {
        const order = [];
        document.querySelectorAll('.lesson-card').forEach((el, index) => {
            const id = el.dataset.id;
            if (id) order.push({ lessonId: id, individualOrderIndex: index });
            el.dataset.order = index;
        });

        if (order.length === 0) return;

        fetch('/Teachingplan/SaveOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value },
            body: JSON.stringify(order)
        })
            .then(response => {
                if (response.ok) {
                    showToast('Reihenfolge gespeichert');
                } else {
                    showToast('Speichern fehlgeschlagen', true);
                }
            })
            .catch(err => {
                console.error('Save order failed', err);
                showToast('Speichern fehlgeschlagen', true);
            });
    }

    function showToast(message, isError) {
        const toast = document.createElement('div');
        toast.className = 'lt-toast';
        toast.textContent = message;
        const bg = isError ? 'rgba(217, 70, 70, 0.95)' : 'rgba(34, 197, 94, 0.95)';
        Object.assign(toast.style, {
            position: 'fixed',
            right: '20px',
            bottom: '20px',
            padding: '10px 14px',
            color: '#fff',
            background: bg,
            borderRadius: '8px',
            boxShadow: '0 6px 18px rgba(0,0,0,0.3)',
            zIndex: 99999,
            fontFamily: 'Arial, sans-serif',
            fontSize: '13px',
            opacity: '0',
            transition: 'opacity 220ms ease, transform 220ms ease'
        });

        document.body.appendChild(toast);
        // trigger show
        requestAnimationFrame(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateY(0)';
        });

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(8px)';
            setTimeout(() => document.body.removeChild(toast), 300);
        }, 2500);
    }

    if (window.Sortable) {
        init();
    } else {
        const s = document.createElement('script');
        s.src = 'https://cdn.jsdelivr.net/npm/sortablejs@1.15.0/Sortable.min.js';
        s.onload = init;
        s.onerror = function () { console.error('Failed to load SortableJS'); };
        document.head.appendChild(s);
    }
})();
