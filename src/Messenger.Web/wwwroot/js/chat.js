window.messengerScrollToEnd = () => {
    const end = document.getElementById("messages-end");
    if (end) {
        end.scrollIntoView({ behavior: "smooth", block: "end" });
    }
};
