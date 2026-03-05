// Chat history persistence via localStorage (survives browser close)
// Storage keys include userId for multi-user readiness (Entra ID)
export const chatStorage = {
    getMessages(userId, agentId) {
        try {
            const key = `aoh-chat-${userId}-${agentId}`;
            const data = localStorage.getItem(key);
            return data ? JSON.parse(data) : [];
        } catch {
            return [];
        }
    },

    setMessages(userId, agentId, messages) {
        try {
            const key = `aoh-chat-${userId}-${agentId}`;
            const trimmed = messages.slice(-15);
            localStorage.setItem(key, JSON.stringify(trimmed));
        } catch { /* storage full or unavailable */ }
    },

    clearMessages(userId, agentId) {
        try {
            localStorage.removeItem(`aoh-chat-${userId}-${agentId}`);
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
