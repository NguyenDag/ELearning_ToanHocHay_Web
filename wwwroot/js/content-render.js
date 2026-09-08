/*
 * content-render.js
 * Hiển thị nội dung bài học được nhập ở dạng Markdown + LaTeX.
 *
 * Cách dùng: đặt văn bản thô (Markdown) vào phần tử và thêm thuộc tính
 *   - data-md         : parse Markdown đầy đủ (đoạn văn, đậm/nghiêng, danh sách, ...)
 *   - data-md-inline  : chỉ parse Markdown trong 1 dòng (dùng cho tiêu đề)
 * Script sẽ tự chuyển thành HTML rồi gọi MathJax để render công thức.
 *
 * Yêu cầu: marked (global "marked") và MathJax v3 đã được nạp trên trang.
 */
(function (global) {
    'use strict';

    var PH_PREFIX = 'xMathJaxSpanx';
    var PH_SUFFIX = 'x';
    var PH_RESTORE = /xMathJaxSpanx(\d+)x/g;
    var NBSP = String.fromCharCode(160);

    // Tách các đoạn công thức ra khỏi chuỗi trước khi parse Markdown,
    // tránh việc marked "ăn" mất dấu \, _, * bên trong công thức.
    function protectMath(src) {
        var store = [];
        var patterns = [
            /\\\[[\s\S]+?\\\]/g, // \[ ... \]
            /\$\$[\s\S]+?\$\$/g, // $$ ... $$
            /\\\([\s\S]+?\\\)/g, // \( ... \)
            /\$[^$\n]+?\$/g       // $ ... $
        ];
        patterns.forEach(function (re) {
            src = src.replace(re, function (m) {
                store.push(m);
                return PH_PREFIX + (store.length - 1) + PH_SUFFIX;
            });
        });
        return { text: src, store: store };
    }

    function restoreMath(html, store) {
        return html.replace(PH_RESTORE, function (_, i) {
            return store[+i] != null ? store[+i] : _;
        });
    }

    function escapeHtml(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function toHtml(raw, inlineOnly) {
        // Chuẩn hoá xuống dòng, thay khoảng trắng cứng (U+00A0) bằng khoảng trắng thường.
        var cleaned = (raw || '')
            .split('\r\n').join('\n')
            .split(NBSP).join(' ')
            .trim();
        if (!cleaned) return '';

        if (inlineOnly) {
            // Bỏ dấu tiêu đề Markdown ở đầu dòng, ví dụ "# Bài 3. ..."
            cleaned = cleaned.replace(/^#{1,6}\s*/, '');
        }

        var p = protectMath(cleaned);
        var out;

        if (global.marked) {
            var opts = { gfm: true, breaks: false };
            if (inlineOnly && typeof global.marked.parseInline === 'function') {
                out = global.marked.parseInline(p.text, opts);
            } else if (typeof global.marked.parse === 'function') {
                out = global.marked.parse(p.text, opts);
            } else {
                out = global.marked(p.text, opts);
            }
        } else {
            // Dự phòng khi marked chưa nạp được: chỉ xử lý đậm/nghiêng cơ bản.
            out = escapeHtml(p.text)
                .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
                .replace(/\*([^*]+)\*/g, '<em>$1</em>');
            if (!inlineOnly) {
                out = '<p>' + out.split(/\n{2,}/).join('</p><p>').split('\n').join('<br>') + '</p>';
            }
        }

        return restoreMath(out, p.store);
    }

    function typesetMath(retries) {
        retries = (retries == null) ? 25 : retries;
        var mj = global.MathJax;
        if (mj && typeof mj.typesetPromise === 'function') {
            mj.typesetPromise().catch(function (e) {
                console.error('MathJax typeset error:', e);
            });
        } else if (retries > 0) {
            setTimeout(function () { typesetMath(retries - 1); }, 200);
        }
    }

    function renderAll(root) {
        root = root || document;
        var nodes = root.querySelectorAll('[data-md], [data-md-inline]');
        Array.prototype.forEach.call(nodes, function (el) {
            if (el.dataset.mdRendered === '1') return;
            el.innerHTML = toHtml(el.textContent, el.hasAttribute('data-md-inline'));
            el.dataset.mdRendered = '1';
        });
        typesetMath();
    }

    global.ContentRender = {
        renderAll: renderAll,
        toHtml: toHtml,
        typesetMath: typesetMath
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { renderAll(); });
    } else {
        renderAll();
    }
})(window);
