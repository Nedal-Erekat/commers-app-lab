import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatMessage } from '../models/chat';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-widget.component.html',
  styleUrl: './chat-widget.component.css'
})
export class ChatWidgetComponent {
  private readonly chatService = inject(ChatService);

  readonly isOpen = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly draft = signal('');
  readonly sending = signal(false);
  readonly error = signal('');

  private conversationId: string | null = null;

  toggle(): void {
    this.isOpen.set(!this.isOpen());
  }

  send(): void {
    const text = this.draft().trim();
    if (!text || this.sending()) return;

    this.messages.update((msgs) => [...msgs, { role: 'user', text }]);
    this.draft.set('');
    this.sending.set(true);
    this.error.set('');

    this.chatService.sendMessage(text, this.conversationId).subscribe({
      next: (response) => {
        this.conversationId = response.conversationId;
        this.messages.update((msgs) => [...msgs, { role: 'assistant', text: response.reply }]);
        this.sending.set(false);
      },
      error: () => {
        this.error.set('The assistant is unavailable right now.');
        this.sending.set(false);
      }
    });
  }
}
