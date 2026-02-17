import { BrowserRouter, Route, Routes } from 'react-router-dom';
import NavBar from './components/NavBar';
import RequireAuth from './components/RequireAuth';

import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import FeedPage from './pages/FeedPage';
import PostDetailPage from './pages/PostDetailPage';
import GroupsPage from './pages/GroupsPage';
import GroupDetailPage from './pages/GroupDetailPage';
import MembersPage from './pages/MembersPage';
import MemberDetailPage from './pages/MemberDetailPage';
import NotificationsPage from './pages/NotificationsPage';
import ModerationPage from './pages/ModerationPage';

export default function App() {
  return (
    <BrowserRouter>
      <NavBar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route
          path="/feed"
          element={
            <RequireAuth>
              <FeedPage />
            </RequireAuth>
          }
        />
        <Route
          path="/posts/:id"
          element={
            <RequireAuth>
              <PostDetailPage />
            </RequireAuth>
          }
        />

        <Route
          path="/groups"
          element={
            <RequireAuth>
              <GroupsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/groups/:id"
          element={
            <RequireAuth>
              <GroupDetailPage />
            </RequireAuth>
          }
        />

        <Route
          path="/members"
          element={
            <RequireAuth>
              <MembersPage />
            </RequireAuth>
          }
        />
        <Route
          path="/members/:id"
          element={
            <RequireAuth>
              <MemberDetailPage />
            </RequireAuth>
          }
        />

        <Route
          path="/notifications"
          element={
            <RequireAuth>
              <NotificationsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/moderation"
          element={
            <RequireAuth>
              <ModerationPage />
            </RequireAuth>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
