export type PageResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  hasMore: boolean;
};

export type PostImageDto = {
  postImageId: string;
  url: string;
  sortOrder: number;
};

export type PostDto = {
  postId: string;
  authorId: string;
  authorName: string;
  bodyText: string;
  linkUrl: string | null;
  linkTitle: string | null;
  linkDescription: string | null;
  linkImageUrl: string | null;
  commentingEnabled: boolean;
  status: string;
  createdAt: string;
  images: PostImageDto[];
  likeCount: number;
  commentCount: number;
  likedByMe: boolean;
  bookmarkedByMe: boolean;
};

export type MemberDto = {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  city: string | null;
  bio: string | null;
  avatarUrl: string | null;
  role: string;
  status: string;
};

export type NotificationDto = {
  notificationId: string;
  type: string;
  payloadJson: string;
  isRead: boolean;
  createdAt: string;
};

export type GroupDto = {
  groupId: string;
  name: string;
  description: string | null;
  visibility: string;
  createdById: string;
  createdAt: string;
  memberCount: number;
  isMember: boolean;
};

export type ReportDto = {
  reportId: string;
  reporterId: string;
  targetType: string;
  targetId: string;
  reason: string;
  notes: string | null;
  status: string;
  createdAt: string;
};

export type MeResponse = {
  userId: string;
  tenantId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  status: string;
};
