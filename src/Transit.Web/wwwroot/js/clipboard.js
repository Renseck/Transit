// Copies text to the clipboard. navigator.clipboard only exists in secure contexts, and Transit is
// served over plain http://transit.local, so fall back to a hidden textarea + execCommand('copy').
window.transitCopyText = async (text) => {
    if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return true;
    }

    const area = document.createElement('textarea');
    area.value = text;
    area.setAttribute('readonly', '');
    area.style.position = 'fixed';
    area.style.opacity = '0';
    document.body.appendChild(area);
    area.select();
    const copied = document.execCommand('copy');
    area.remove();
    return copied;
};
