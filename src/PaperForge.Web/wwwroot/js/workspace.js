(function () {
    'use strict';

    const { paperId, citationStyle, sectionData } = window.PaperForge;
    let currentSectionId = null;
    let saveTimer = null;
    let isDirty = false;

    // In-memory content cache (keyed by sectionId)
    const contentCache = {};
    if (sectionData) {
        for (const [id, data] of Object.entries(sectionData)) {
            contentCache[id] = data.content || '';
        }
    }
    console.log('[PaperForge] contentCache keys:', Object.keys(contentCache));
    console.log('[PaperForge] first content preview:', Object.values(contentCache)[0]?.substring(0, 200));

    // ═══════════════════════════════════
    // QUILL EDITOR
    // ═══════════════════════════════════

    // Build modules config — syntax and formula are optional
    const modules = {
        toolbar: [
            [{ header: [1, 2, 3, false] }],
            [{ font: [] }],
            [{ size: ['small', false, 'large', 'huge'] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ script: 'sub' }, { script: 'super' }],
            [{ color: [] }, { background: [] }],
            [{ align: [] }],
            [{ list: 'ordered' }, { list: 'bullet' }],
            [{ indent: '-1' }, { indent: '+1' }],
            ['blockquote', 'code-block'],
            ['link', 'image'],
            ['clean'],
        ],
    };

    // Only add optional modules if their dependencies loaded
    if (window.hljs) modules.syntax = { hljs: window.hljs };

    const quill = new Quill('#quillEditor', {
        theme: 'snow',
        modules,
        placeholder: 'Start writing...',
    });
    console.log('[PaperForge] Quill initialized');

    // ═══════════════════════════════════
    // SECTION SWITCHING (event delegation)
    // ═══════════════════════════════════

    const sectionList = document.getElementById('sectionList');
    const sectionTitle = document.getElementById('currentSectionTitle');
    const guidanceText = document.getElementById('guidanceText');

    function getAllSectionItems() {
        return sectionList.querySelectorAll('.section-item');
    }

    function selectSection(sectionId) {
        // Save current section first
        if (currentSectionId && isDirty) {
            saveSection(currentSectionId);
        }

        currentSectionId = sectionId;

        // Update active class
        getAllSectionItems().forEach(item => {
            item.classList.toggle('active', item.dataset.sectionId === sectionId);
        });

        // Load content from cache
        const content = contentCache[sectionId] || '';
        if (content) {
            try {
                const delta = JSON.parse(content);
                quill.setContents(delta);
            } catch {
                quill.setText(content);
            }
        } else {
            quill.setText('');
        }

        // Update title
        const activeItem = document.querySelector(`.section-item[data-section-id="${sectionId}"]`);
        if (activeItem) {
            sectionTitle.textContent = activeItem.querySelector('.section-title').textContent;
        }

        // Update guidance
        const data = sectionData?.[sectionId];
        if (data && data.guidance) {
            guidanceText.textContent = data.guidance;
        } else {
            guidanceText.textContent = 'No guidance for this section.';
        }

        isDirty = false;
        updateWordCount();
    }

    // Event delegation for section clicks
    sectionList.addEventListener('click', e => {
        // Ignore clicks on delete button
        if (e.target.closest('.btn-delete-section')) return;
        const item = e.target.closest('.section-item');
        if (item) selectSection(item.dataset.sectionId);
    });

    // Select first section on load
    const firstItem = sectionList.querySelector('.section-item');
    if (firstItem) selectSection(firstItem.dataset.sectionId);

    // ═══════════════════════════════════
    // SECTION MANAGEMENT (add, rename, delete)
    // ═══════════════════════════════════

    // --- ADD SECTION ---
    document.getElementById('btnAddSection')?.addEventListener('click', () => {
        const title = prompt('Section name:');
        if (!title?.trim()) return;

        fetch('/api/Section/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ paperId, title: title.trim() }),
        })
            .then(r => { if (!r.ok) throw new Error('Failed'); return r.json(); })
            .then(sec => {
                // Add to DOM
                const li = document.createElement('li');
                li.className = 'section-item';
                li.dataset.sectionId = sec.id;
                li.dataset.order = sec.orderIndex;
                li.innerHTML = `
                    <div class="d-flex justify-content-between align-items-start">
                        <span class="section-title" title="Double-click to rename">${esc(sec.title)}</span>
                        <div class="d-flex align-items-center gap-1">
                            <span class="badge bg-secondary badge-sm">NotStarted</span>
                            <button class="btn btn-sm btn-link text-danger p-0 btn-delete-section" title="Delete section">
                                <i class="bi bi-x"></i>
                            </button>
                        </div>
                    </div>
                    <small class="text-muted word-count">0 words</small>
                `;
                sectionList.appendChild(li);
                contentCache[sec.id] = '';
                selectSection(sec.id);
            })
            .catch(() => alert('Failed to add section.'));
    });

    // --- DELETE SECTION ---
    sectionList.addEventListener('click', e => {
        const btn = e.target.closest('.btn-delete-section');
        if (!btn) return;
        e.stopPropagation();

        const item = btn.closest('.section-item');
        const sectionId = item.dataset.sectionId;
        const title = item.querySelector('.section-title').textContent;

        if (!confirm(`Delete section "${title}"?`)) return;

        fetch(`/api/Section/${sectionId}`, { method: 'DELETE' })
            .then(r => {
                if (!r.ok) throw new Error('Failed');
                item.remove();
                delete contentCache[sectionId];

                // If we deleted the active section, select another
                if (currentSectionId === sectionId) {
                    const next = sectionList.querySelector('.section-item');
                    if (next) {
                        selectSection(next.dataset.sectionId);
                    } else {
                        currentSectionId = null;
                        quill.setText('');
                        sectionTitle.textContent = 'No sections';
                    }
                }
            })
            .catch(() => alert('Failed to delete section.'));
    });

    // --- RENAME SECTION (double-click) ---
    sectionList.addEventListener('dblclick', e => {
        const titleEl = e.target.closest('.section-title');
        if (!titleEl) return;

        const item = titleEl.closest('.section-item');
        const sectionId = item.dataset.sectionId;
        const oldTitle = titleEl.textContent.trim();

        // Replace span with input
        const input = document.createElement('input');
        input.type = 'text';
        input.value = oldTitle;
        input.className = 'form-control form-control-sm section-rename-input';
        titleEl.replaceWith(input);
        input.focus();
        input.select();

        function commitRename() {
            const newTitle = input.value.trim() || oldTitle;
            const span = document.createElement('span');
            span.className = 'section-title';
            span.title = 'Double-click to rename';
            span.textContent = newTitle;
            input.replaceWith(span);

            if (newTitle !== oldTitle) {
                fetch(`/api/Section/${sectionId}/rename`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ title: newTitle }),
                }).catch(() => {
                    span.textContent = oldTitle;
                    alert('Failed to rename section.');
                });

                // Update editor header if this is the active section
                if (currentSectionId === sectionId) {
                    sectionTitle.textContent = newTitle;
                }
            }
        }

        input.addEventListener('blur', commitRename);
        input.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); input.blur(); }
            if (e.key === 'Escape') { input.value = oldTitle; input.blur(); }
        });
    });

    // ═══════════════════════════════════
    // AUTO-SAVE (every 5s when dirty)
    // ═══════════════════════════════════

    const statusEl = document.getElementById('autoSaveStatus');

    quill.on('text-change', () => {
        isDirty = true;
        statusEl.innerHTML = '<i class="bi bi-pencil text-warning"></i> Editing...';

        clearTimeout(saveTimer);
        saveTimer = setTimeout(() => {
            if (currentSectionId && isDirty) {
                saveSection(currentSectionId);
            }
        }, 5000);

        updateWordCount();
    });

    function saveSection(sectionId) {
        const delta = JSON.stringify(quill.getContents());
        const plainText = quill.getText().trim();

        // Store in local cache
        contentCache[sectionId] = delta;

        statusEl.innerHTML = '<i class="bi bi-arrow-repeat text-info"></i> Saving...';

        fetch(`/api/Section/${sectionId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ content: delta, plainText }),
        })
            .then(r => {
                if (r.ok) {
                    statusEl.innerHTML = '<i class="bi bi-check-circle text-success"></i> Saved';
                    isDirty = false;
                    updateSectionStatus(sectionId, plainText);
                } else {
                    statusEl.innerHTML = '<i class="bi bi-exclamation-circle text-danger"></i> Save failed';
                }
            })
            .catch(() => {
                statusEl.innerHTML = '<i class="bi bi-exclamation-circle text-danger"></i> Save failed';
            });
    }

    // Public method for export buttons
    window.AutoSave = {
        forceSave() {
            return new Promise(resolve => {
                if (currentSectionId && isDirty) {
                    const sectionId = currentSectionId;
                    const delta = JSON.stringify(quill.getContents());
                    const plainText = quill.getText().trim();
                    contentCache[sectionId] = delta;

                    fetch(`/api/Section/${sectionId}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ content: delta, plainText }),
                    }).then(() => { isDirty = false; resolve(); })
                      .catch(() => resolve());
                } else {
                    resolve();
                }
            });
        },
    };

    // ═══════════════════════════════════
    // WORD COUNT
    // ═══════════════════════════════════

    function updateWordCount() {
        if (!currentSectionId) return;
        const text = quill.getText().trim();
        const count = text ? text.split(/\s+/).length : 0;
        const item = document.querySelector(`.section-item[data-section-id="${currentSectionId}"]`);
        if (!item) return;
        const wcEl = item.querySelector('.word-count');
        wcEl.textContent = `${count} word${count !== 1 ? 's' : ''}`;
    }

    function updateSectionStatus(sectionId, plainText) {
        const item = document.querySelector(`.section-item[data-section-id="${sectionId}"]`);
        if (!item) return;
        const badge = item.querySelector('.badge');
        if (plainText) {
            badge.className = 'badge bg-warning badge-sm';
            badge.textContent = 'InProgress';
        } else {
            badge.className = 'badge bg-secondary badge-sm';
            badge.textContent = 'NotStarted';
        }
    }

    // ═══════════════════════════════════
    // GUIDANCE TOGGLE
    // ═══════════════════════════════════

    document.getElementById('toggleGuidance')?.addEventListener('click', () => {
        const panel = document.getElementById('guidancePanel');
        panel.classList.toggle('collapsed');
        const icon = document.querySelector('#toggleGuidance i');
        icon.className = panel.classList.contains('collapsed')
            ? 'bi bi-chevron-down' : 'bi bi-chevron-up';
    });

    // ═══════════════════════════════════
    // EXPORT
    // ═══════════════════════════════════

    function exportPaper(format, studentPaper) {
        window.AutoSave.forceSave().then(() => {
            window.location.href = `/api/Export/download/${paperId}?format=${format}&studentPaper=${studentPaper}`;
        });
    }

    document.getElementById('btnExportPdfStudent')?.addEventListener('click', e => {
        e.preventDefault();
        exportPaper(0, true);
    });
    document.getElementById('btnExportDocxStudent')?.addEventListener('click', e => {
        e.preventDefault();
        exportPaper(1, true);
    });
    document.getElementById('btnExportPdfPro')?.addEventListener('click', e => {
        e.preventDefault();
        exportPaper(0, false);
    });
    document.getElementById('btnExportDocxPro')?.addEventListener('click', e => {
        e.preventDefault();
        exportPaper(1, false);
    });

    // ═══════════════════════════════════
    // REFERENCES
    // ═══════════════════════════════════

    const refForm = document.getElementById('refForm');
    const refList = document.getElementById('referenceList');

    document.getElementById('btnAddRef')?.addEventListener('click', () => {
        refForm.classList.toggle('d-none');
    });

    document.getElementById('btnCancelRef')?.addEventListener('click', () => {
        refForm.classList.add('d-none');
        clearRefForm();
    });

    document.getElementById('btnSaveRef')?.addEventListener('click', () => {
        const ref = {
            referenceType: parseInt(document.getElementById('refType').value),
            authorLastName: document.getElementById('refAuthorLast').value.trim(),
            authorFirstName: document.getElementById('refAuthorFirst').value.trim(),
            title: document.getElementById('refTitle').value.trim(),
            year: parseInt(document.getElementById('refYear').value) || null,
            publisher: document.getElementById('refPublisher').value.trim() || null,
            journal: document.getElementById('refJournal').value.trim() || null,
            volume: document.getElementById('refVolume').value.trim() || null,
            issue: document.getElementById('refIssue').value.trim() || null,
            pages: document.getElementById('refPages').value.trim() || null,
            doi: document.getElementById('refDoi').value.trim() || null,
            url: document.getElementById('refUrl').value.trim() || null,
        };

        if (!ref.authorLastName && !ref.title) {
            alert('Author last name or title is required.');
            return;
        }

        fetch(`/api/Reference/${paperId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ref),
        })
            .then(r => {
                if (!r.ok) return r.json().then(err => { throw new Error(err.error || 'Save failed'); });
                return r.json();
            })
            .then(saved => {
                addRefToList(saved);
                refForm.classList.add('d-none');
                clearRefForm();
            })
            .catch(err => alert(err.message || 'Failed to save reference.'));
    });

    // DOI Lookup
    document.getElementById('btnDoiLookup')?.addEventListener('click', () => {
        const doi = document.getElementById('doiInput').value.trim();
        if (!doi) return;

        const btn = document.getElementById('btnDoiLookup');
        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-hourglass-split"></i>';

        fetch(`/api/Reference/doi/${encodeURIComponent(doi)}`)
            .then(r => {
                if (!r.ok) return r.json().then(err => { throw new Error(err.error || 'DOI not found'); });
                return r.json();
            })
            .then(data => {
                document.getElementById('refAuthorLast').value = data.authorLastName || '';
                document.getElementById('refAuthorFirst').value = data.authorFirstName || '';
                document.getElementById('refTitle').value = data.title || '';
                document.getElementById('refYear').value = data.year || '';
                document.getElementById('refPublisher').value = data.publisher || '';
                document.getElementById('refJournal').value = data.journal || '';
                document.getElementById('refVolume').value = data.volume || '';
                document.getElementById('refIssue').value = data.issue || '';
                document.getElementById('refPages').value = data.pages || '';
                document.getElementById('refDoi').value = data.doi || doi;
                document.getElementById('refType').value = data.journal ? '1' : '0';
                refForm.classList.remove('d-none');
            })
            .catch(err => alert(err.message || 'DOI lookup failed.'))
            .finally(() => {
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-search"></i>';
            });
    });

    // Delete reference
    refList?.addEventListener('click', e => {
        const btn = e.target.closest('.btn-delete-ref');
        if (!btn) return;
        const item = btn.closest('.reference-item');
        const refId = item.dataset.refId;

        fetch(`/api/Reference/${refId}`, { method: 'DELETE' })
            .then(() => item.remove())
            .catch(() => alert('Failed to delete reference.'));
    });

    function addRefToList(ref) {
        const li = document.createElement('li');
        li.className = 'reference-item';
        li.dataset.refId = ref.id;
        li.innerHTML = `
            <div class="d-flex justify-content-between">
                <small class="fw-bold">${esc(ref.authorLastName)}, ${esc(ref.authorFirstName)}</small>
                <button class="btn btn-sm btn-link text-danger p-0 btn-delete-ref">
                    <i class="bi bi-x"></i>
                </button>
            </div>
            <small class="text-muted">${esc(ref.title)} (${ref.year || 'n.d.'})</small>
        `;
        refList.appendChild(li);
    }

    function clearRefForm() {
        ['refAuthorLast', 'refAuthorFirst', 'refTitle', 'refYear',
         'refPublisher', 'refJournal', 'refVolume', 'refIssue',
         'refPages', 'refDoi', 'refUrl'].forEach(id => {
            document.getElementById(id).value = '';
        });
        document.getElementById('refType').value = '0';
    }

    function esc(str) {
        const div = document.createElement('div');
        div.textContent = str || '';
        return div.innerHTML;
    }
})();
