// Chat history persistence via sessionStorage (survives refresh, clears on tab close)
export const chatStorage = {
    getMessages(agentId) {
        try {
            const key = `aoh-chat-${agentId}`;
            const data = sessionStorage.getItem(key);
            return data ? JSON.parse(data) : [];
        } catch {
            return [];
        }
    },

    setMessages(agentId, messages) {
        try {
            const key = `aoh-chat-${agentId}`;
            const trimmed = messages.slice(-15);
            sessionStorage.setItem(key, JSON.stringify(trimmed));
        } catch { /* storage full or unavailable */ }
    },

    clearMessages(agentId) {
        try {
            sessionStorage.removeItem(`aoh-chat-${agentId}`);
        } catch { /* ignore */ }
    },

    scrollToBottom(element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    buildShareMailto(agentName, messages) {
        const subject = encodeURIComponent(`Chat with ${agentName} — AgentOpsHub`);
        const lines = messages.map(m => {
            const role = m.role === 0 ? 'You' : agentName;
            const time = new Date(m.timestamp).toLocaleString();
            return `[${time}] ${role}:\n${m.content}\n`;
        });
        const body = encodeURIComponent(lines.join('\n---\n'));
        return `mailto:?subject=${subject}&body=${body}`;
    }
};
