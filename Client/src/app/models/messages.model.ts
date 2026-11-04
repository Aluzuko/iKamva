import { UserRole } from './user-role.model';

export interface Conversation {
  conversationId: string;
  otherUserId: string;
  otherName: string;
  otherRole: UserRole;
  lastMessage: string | null;
  lastMessageAt: string | null;
  lastMessageMine: boolean;
  unreadCount: number;
}

export interface ChatMessage {
  messageId: string;
  body: string;
  sentAt: string;
  mine: boolean;
  readAt: string | null;
}

export interface MessagePage {
  conversation: Conversation;
  messages: ChatMessage[];
  hasMore: boolean;
}

export interface Contact {
  userId: string;
  fullName: string;
  role: UserRole;
  context: string;
}

export const MAX_MESSAGE_LENGTH = 2000;
