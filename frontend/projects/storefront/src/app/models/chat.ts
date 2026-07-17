export interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

export interface ChatResponse {
  conversationId: string;
  reply: string;
  toolsUsed: string[];
}
