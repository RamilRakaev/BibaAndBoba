const COMPOSER_ID = "messenger-draft";
let liftTimers = [];

const setVar = (name, value) => {
    document.documentElement.style.setProperty(name, value);
};

const draftEl = () => document.getElementById(COMPOSER_ID);

const draftText = (el) => (el?.innerText || "").replace(/\u00a0/g, " ");

const syncEmptyClass = (el) => {
    if (!el) {
        return;
    }
    el.classList.toggle("is-empty", draftText(el).trim() === "");
};

window.messengerScrollToEnd = () => {
    const end = document.getElementById("messages-end");
    if (end) {
        end.scrollIntoView({ behavior: "smooth", block: "end" });
    }
};

window.messengerReadDraft = () => draftText(draftEl());

window.messengerClearDraft = () => {
    const el = draftEl();
    if (el) {
        el.innerHTML = "";
        syncEmptyClass(el);
    }
};

window.messengerFocusDraft = () => {
    const el = draftEl();
    if (el) {
        el.focus({ preventScroll: true });
        liftComposer();
    }
};

const syncVisualViewport = () => {
    const vv = window.visualViewport;
    const height = vv ? vv.height : window.innerHeight;
    const offsetTop = vv ? vv.offsetTop : 0;
    setVar("--vv-height", `${Math.round(height)}px`);
    setVar("--vv-offset-top", `${Math.round(offsetTop)}px`);
};

const clearLiftTimers = () => {
    for (const id of liftTimers) {
        clearTimeout(id);
    }
    liftTimers = [];
};

const liftComposer = () => {
    const input = draftEl();
    const bar = input?.closest(".messenger-input");
    if (!input || !bar) {
        setVar("--composer-lift", "0px");
        return;
    }

    syncVisualViewport();
    window.scrollTo(0, 0);

    const vv = window.visualViewport;
    const visibleBottom = vv ? vv.height : window.innerHeight;
    const rect = bar.getBoundingClientRect();
    const overflow = rect.bottom - visibleBottom;
    const lift = overflow > 1 ? Math.ceil(overflow) + 8 : 0;
    setVar("--composer-lift", `${lift}px`);
    input.scrollIntoView({ block: "nearest", inline: "nearest" });
};

const scheduleLift = () => {
    clearLiftTimers();
    liftComposer();
    liftTimers = [50, 150, 400, 900, 1800, 2800].map((ms) =>
        setTimeout(liftComposer, ms)
    );
};

const onComposerFocus = () => {
    document.documentElement.classList.add("composer-focused");
    scheduleLift();
};

const onComposerBlur = () => {
    document.documentElement.classList.remove("composer-focused");
    clearLiftTimers();
    setVar("--composer-lift", "0px");
    syncVisualViewport();
};

document.addEventListener("focusin", (event) => {
    if (event.target?.id === COMPOSER_ID) {
        onComposerFocus();
    }
});

document.addEventListener("focusout", (event) => {
    if (event.target?.id === COMPOSER_ID) {
        onComposerBlur();
    }
});

document.addEventListener("input", (event) => {
    if (event.target?.id !== COMPOSER_ID) {
        return;
    }
    const el = event.target;
    const text = draftText(el);
    if (text.length > 4000) {
        el.innerText = text.slice(0, 4000);
    }
    syncEmptyClass(el);
    liftComposer();
});

document.addEventListener("keydown", (event) => {
    if (event.target?.id !== COMPOSER_ID) {
        return;
    }
    if (event.key === "Enter" && !event.shiftKey) {
        event.preventDefault();
        document.getElementById("messenger-send")?.click();
    }
});

document.addEventListener("paste", (event) => {
    if (event.target?.id !== COMPOSER_ID) {
        return;
    }
    event.preventDefault();
    const text = event.clipboardData?.getData("text/plain") ?? "";
    document.execCommand("insertText", false, text);
    syncEmptyClass(event.target);
});

window.visualViewport?.addEventListener("resize", () => {
    syncVisualViewport();
    if (document.documentElement.classList.contains("composer-focused")) {
        liftComposer();
    }
});

window.visualViewport?.addEventListener("scroll", () => {
    syncVisualViewport();
    if (document.documentElement.classList.contains("composer-focused")) {
        window.scrollTo(0, 0);
        liftComposer();
    }
});

window.addEventListener("resize", syncVisualViewport);
window.addEventListener("orientationchange", () => setTimeout(syncVisualViewport, 200));

syncVisualViewport();
