import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ApiError, deleteLink, getLinks } from '../api/client';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { CopyButton } from '../components/CopyButton';
import { EmptyState } from '../components/EmptyState';
import { Icon } from '../components/Icon';
import { LinkListSkeleton } from '../components/LinkListSkeleton';
import { useAuth } from '../auth/useAuth';
import { useToast } from '../toast/useToast';
import { buildShortLink, formatTimestamp, safeExternalUrl } from '../lib/url';
import type { ShortUrl } from '../types';

type SortOption = 'newest' | 'key' | 'domain';

const SORT_LABELS: Record<SortOption, string> = {
  newest: 'Newest first',
  key: 'Short key (A–Z)',
  domain: 'Domain (A–Z)',
};

function linkId(link: ShortUrl): string {
  return `${link.rowKey}/${link.partitionKey}`;
}

export function LinksPage() {
  const { notify } = useToast();
  const { signIn } = useAuth();

  const [links, setLinks] = useState<ShortUrl[]>([]);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>('loading');
  const [errorMessage, setErrorMessage] = useState('');
  const [query, setQuery] = useState('');
  const [sort, setSort] = useState<SortOption>('newest');
  const [pendingDelete, setPendingDelete] = useState<ShortUrl | null>(null);
  const [deleting, setDeleting] = useState(false);

  const handleApiError = useCallback(
    (error: unknown, fallback: string) => {
      const message = error instanceof ApiError ? error.message : fallback;

      if (error instanceof ApiError && error.isAuthError) {
        signIn(window.location.pathname);
      }

      return message;
    },
    [signIn],
  );

  const load = useCallback(
    async (signal?: AbortSignal) => {
      setStatus('loading');

      try {
        const result = await getLinks(signal);

        if (signal?.aborted) {
          return;
        }

        setLinks(result);
        setStatus('ready');
      } catch (error) {
        if (signal?.aborted) {
          return;
        }

        setErrorMessage(handleApiError(error, 'Your short links could not be loaded.'));
        setStatus('error');
      }
    },
    [handleApiError],
  );

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      await load(controller.signal);
    })();

    return () => controller.abort();
  }, [load]);

  const visibleLinks = useMemo(() => {
    const needle = query.trim().toLowerCase();

    const filtered = needle
      ? links.filter((link) =>
          [link.partitionKey, link.rowKey, link.url].some((value) =>
            value.toLowerCase().includes(needle),
          ),
        )
      : links;

    return [...filtered].sort((a, b) => {
      if (sort === 'key') {
        return a.partitionKey.localeCompare(b.partitionKey);
      }

      if (sort === 'domain') {
        return a.rowKey.localeCompare(b.rowKey) || a.partitionKey.localeCompare(b.partitionKey);
      }

      const aTime = a.timestamp ? Date.parse(a.timestamp) : 0;
      const bTime = b.timestamp ? Date.parse(b.timestamp) : 0;

      return bTime - aTime;
    });
  }, [links, query, sort]);

  const confirmDelete = async () => {
    if (!pendingDelete) {
      return;
    }

    setDeleting(true);

    try {
      await deleteLink(pendingDelete);
      setLinks((current) => current.filter((link) => linkId(link) !== linkId(pendingDelete)));
      notify(`Deleted ${buildShortLink(pendingDelete.rowKey, pendingDelete.partitionKey)}.`, 'success');
      setPendingDelete(null);
    } catch (error) {
      notify(handleApiError(error, 'The short link could not be deleted.'), 'error');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <header className="page-header page-header--row">
        <div>
          <h1 className="page-header__title">My short links</h1>
          <p className="page-header__subtitle">
            {status === 'ready'
              ? `${links.length} link${links.length === 1 ? '' : 's'} created with your account.`
              : 'Every short link created with your account.'}
          </p>
        </div>
        <div className="form-actions">
          <button
            type="button"
            className="btn btn--secondary"
            onClick={() => void load()}
            disabled={status === 'loading'}
          >
            <Icon name="refresh" size={16} />
            Refresh
          </button>
          <Link className="btn btn--primary" to="/">
            <Icon name="plus" size={16} />
            New link
          </Link>
        </div>
      </header>

      {status === 'ready' && links.length > 0 && (
        <div className="list-toolbar">
          <div className="field list-toolbar__search">
            <label className="visually-hidden" htmlFor="search">
              Search links
            </label>
            <input
              id="search"
              className="input"
              type="search"
              placeholder="Search by key, domain or destination"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </div>
          <div className="field">
            <label className="visually-hidden" htmlFor="sort">
              Sort links
            </label>
            <select
              id="sort"
              className="select"
              value={sort}
              onChange={(event) => setSort(event.target.value as SortOption)}
            >
              {Object.entries(SORT_LABELS).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
        </div>
      )}

      {status === 'loading' && <LinkListSkeleton />}

      {status === 'error' && (
        <EmptyState icon="alert" title="Something went wrong" description={errorMessage}>
          <button type="button" className="btn btn--primary" onClick={() => void load()}>
            <Icon name="refresh" size={16} />
            Try again
          </button>
        </EmptyState>
      )}

      {status === 'ready' && links.length === 0 && (
        <EmptyState
          title="No short links yet"
          description="Create your first short link and it will show up here."
        >
          <Link className="btn btn--primary" to="/">
            <Icon name="plus" size={16} />
            Create a short link
          </Link>
        </EmptyState>
      )}

      {status === 'ready' && links.length > 0 && visibleLinks.length === 0 && (
        <EmptyState
          icon="search"
          title="No matches"
          description={`No short link matches “${query.trim()}”.`}
        >
          <button type="button" className="btn btn--secondary" onClick={() => setQuery('')}>
            Clear search
          </button>
        </EmptyState>
      )}

      {status === 'ready' && visibleLinks.length > 0 && (
        <ul className="link-grid">
          {visibleLinks.map((link) => {
            const shortLink = buildShortLink(link.rowKey, link.partitionKey);
            const target = safeExternalUrl(link.url);

            return (
              <li className="link-card" key={linkId(link)}>
                <div className="link-card__top">
                  <a
                    className="link-card__short"
                    href={shortLink}
                    target="_blank"
                    rel="noreferrer noopener"
                  >
                    {link.rowKey}/{link.partitionKey}
                  </a>
                </div>

                <p className="link-card__target">
                  <Icon name="link" size={16} />
                  {target ? (
                    <a href={target} target="_blank" rel="noreferrer noopener">
                      {link.url}
                    </a>
                  ) : (
                    <span>{link.url || 'No destination stored'}</span>
                  )}
                </p>

                <p className="link-card__meta">Last updated {formatTimestamp(link.timestamp)}</p>

                <div className="link-card__actions">
                  <CopyButton value={shortLink} />
                  <a
                    className="btn btn--secondary btn--small"
                    href={shortLink}
                    target="_blank"
                    rel="noreferrer noopener"
                  >
                    <Icon name="external" size={16} />
                    Open
                  </a>
                  <button
                    type="button"
                    className="btn btn--danger btn--small"
                    onClick={() => setPendingDelete(link)}
                  >
                    <Icon name="trash" size={16} />
                    Delete
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Delete this short link?"
        description={
          pendingDelete
            ? `${buildShortLink(pendingDelete.rowKey, pendingDelete.partitionKey)} will stop redirecting immediately. This cannot be undone.`
            : undefined
        }
        confirmLabel="Delete"
        busy={deleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setPendingDelete(null)}
      />
    </>
  );
}
