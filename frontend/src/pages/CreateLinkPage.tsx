import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { ApiError, createLink, getDomains, getLinks } from '../api/client';
import { CopyButton } from '../components/CopyButton';
import { Icon } from '../components/Icon';
import { useAuth } from '../auth/useAuth';
import { useToast } from '../toast/useToast';
import {
  SHORT_KEY_MAX_LENGTH,
  SHORT_KEY_PATTERN,
  buildShortLink,
  generateShortKey,
  safeExternalUrl,
  withScheme,
} from '../lib/url';
import type { ConfigurationEntry, ShortUrl } from '../types';

interface FieldErrors {
  url?: string;
  domain?: string;
  key?: string;
}

interface CreatedLink {
  shortLink: string;
  target: string;
}

function validateFields(url: string, domain: string, key: string): FieldErrors {
  const errors: FieldErrors = {};
  const normalizedUrl = withScheme(url);

  if (!normalizedUrl) {
    errors.url = 'Enter the address the short link should point to.';
  } else if (!safeExternalUrl(normalizedUrl)) {
    errors.url = 'Enter a valid http:// or https:// address.';
  }

  if (!domain) {
    errors.domain = 'Choose a domain.';
  }

  if (!key) {
    errors.key = 'Enter a short key, or generate one.';
  } else if (key.length > SHORT_KEY_MAX_LENGTH) {
    errors.key = `Use at most ${SHORT_KEY_MAX_LENGTH} characters.`;
  } else if (!SHORT_KEY_PATTERN.test(key)) {
    errors.key = 'Use letters, numbers, hyphens and underscores only.';
  }

  return errors;
}

export function CreateLinkPage() {
  // The destination can be pre-filled through `?url=` so the app works as a
  // bookmarklet target.
  const [searchParams] = useSearchParams();
  const { notify } = useToast();
  const { signIn } = useAuth();

  const [url, setUrl] = useState(() => searchParams.get('url') ?? '');
  const [domain, setDomain] = useState('');
  const [key, setKey] = useState('');

  const [domains, setDomains] = useState<ConfigurationEntry[]>([]);
  const [domainsState, setDomainsState] = useState<'loading' | 'ready' | 'error'>('loading');
  const [existingLinks, setExistingLinks] = useState<ShortUrl[]>([]);

  const [showErrors, setShowErrors] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<CreatedLink | null>(null);

  const urlInputRef = useRef<HTMLInputElement>(null);

  const handleApiError = useCallback(
    (error: unknown, fallback: string) => {
      if (error instanceof ApiError && error.isAuthError) {
        notify(error.message, 'error');
        signIn(window.location.pathname + window.location.search);
        return;
      }

      notify(error instanceof ApiError ? error.message : fallback, 'error');
    },
    [notify, signIn],
  );

  useEffect(() => {
    const controller = new AbortController();

    getDomains(controller.signal)
      .then((entries) => {
        setDomains(entries);
        setDomainsState('ready');
        setDomain((current) => current || (entries.length === 1 ? entries[0].rowKey : ''));
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          return;
        }

        setDomainsState('error');
        handleApiError(error, 'The list of domains could not be loaded.');
      });

    return () => controller.abort();
  }, [handleApiError]);

  // Used only to warn about keys that are already taken; failures are non-fatal.
  useEffect(() => {
    const controller = new AbortController();

    getLinks(controller.signal)
      .then(setExistingLinks)
      .catch(() => undefined);

    return () => controller.abort();
  }, []);

  useEffect(() => {
    urlInputRef.current?.focus();
  }, []);

  const trimmedKey = key.trim();

  const duplicate = useMemo(
    () =>
      Boolean(domain) &&
      Boolean(trimmedKey) &&
      existingLinks.some((link) => link.rowKey === domain && link.partitionKey === trimmedKey),
    [domain, existingLinks, trimmedKey],
  );

  const preview = buildShortLink(domain, trimmedKey);

  // Validation runs on every change, but messages stay hidden until the first
  // submit so the form does not shout at the user while they are still typing.
  const validation = useMemo(
    () => validateFields(url, domain, trimmedKey),
    [domain, trimmedKey, url],
  );
  const errors: FieldErrors = showErrors ? validation : {};

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setShowErrors(true);

    if (Object.keys(validation).length > 0) {
      return;
    }

    const target = safeExternalUrl(withScheme(url)) ?? '';
    const payload: ShortUrl = { partitionKey: trimmedKey, rowKey: domain, url: target };

    setSubmitting(true);

    try {
      await createLink(payload);
      setCreated({ shortLink: buildShortLink(domain, trimmedKey), target });
      setExistingLinks((current) => [
        ...current.filter(
          (link) => !(link.rowKey === domain && link.partitionKey === trimmedKey),
        ),
        payload,
      ]);
      setUrl('');
      setKey('');
      setShowErrors(false);
      notify('Short link created.', 'success');
    } catch (error) {
      handleApiError(error, 'The short link could not be created.');
    } finally {
      setSubmitting(false);
    }
  };

  const startOver = () => {
    setCreated(null);
    setShowErrors(false);
    urlInputRef.current?.focus();
  };

  return (
    <>
      <header className="page-header">
        <h1 className="page-header__title">Create a short link</h1>
        <p className="page-header__subtitle">
          Point a memorable key on one of your domains at any destination.
        </p>
      </header>

      <div className="card">
        <form className="stack" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label className="field__label" htmlFor="destination">
              Destination URL
            </label>
            <input
              id="destination"
              ref={urlInputRef}
              className="input"
              type="url"
              inputMode="url"
              autoComplete="url"
              placeholder="https://example.com/a-very-long-address"
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              aria-invalid={errors.url ? 'true' : undefined}
              aria-describedby={errors.url ? 'destination-error' : undefined}
            />
            {errors.url && (
              <p className="field__error" id="destination-error">
                {errors.url}
              </p>
            )}
          </div>

          <div className="field-grid">
            <div className="field">
              <label className="field__label" htmlFor="domain">
                Domain
              </label>
              <select
                id="domain"
                className="select"
                value={domain}
                onChange={(event) => setDomain(event.target.value)}
                disabled={domainsState === 'loading' || domains.length === 0}
                aria-invalid={errors.domain ? 'true' : undefined}
                aria-describedby={errors.domain ? 'domain-error' : undefined}
              >
                <option value="">
                  {domainsState === 'loading' ? 'Loading domains…' : 'Select a domain'}
                </option>
                {domains.map((entry) => (
                  <option key={entry.rowKey} value={entry.rowKey}>
                    {entry.rowKey}
                  </option>
                ))}
              </select>
              {errors.domain && (
                <p className="field__error" id="domain-error">
                  {errors.domain}
                </p>
              )}
              {domainsState === 'error' && (
                <p className="field__error">Domains could not be loaded. Try reloading the page.</p>
              )}
            </div>

            <div className="field">
              <label className="field__label" htmlFor="key">
                Short key
              </label>
              <div className="input-group">
                <input
                  id="key"
                  className="input"
                  type="text"
                  autoComplete="off"
                  spellCheck={false}
                  maxLength={SHORT_KEY_MAX_LENGTH}
                  placeholder="docs"
                  value={key}
                  onChange={(event) => setKey(event.target.value)}
                  aria-invalid={errors.key ? 'true' : undefined}
                  aria-describedby={errors.key ? 'key-error' : 'key-hint'}
                />
                <button
                  type="button"
                  className="btn btn--secondary"
                  onClick={() => setKey(generateShortKey())}
                  title="Generate a random key"
                >
                  <Icon name="shuffle" size={16} />
                  Random
                </button>
              </div>
              {errors.key ? (
                <p className="field__error" id="key-error">
                  {errors.key}
                </p>
              ) : (
                <p className="field__hint" id="key-hint">
                  {preview ? `Your link: ${preview}` : 'Letters, numbers, hyphens and underscores.'}
                </p>
              )}
            </div>
          </div>

          {duplicate && (
            <p className="alert alert--warning">
              <Icon name="alert" size={18} />
              <span>
                <strong>{trimmedKey}</strong> already exists on {domain}. Saving will replace the
                existing destination.
              </span>
            </p>
          )}

          <div className="form-actions">
            <button type="submit" className="btn btn--primary" disabled={submitting}>
              {submitting ? <span className="spinner" aria-hidden="true" /> : <Icon name="plus" size={16} />}
              {submitting ? 'Creating…' : 'Create short link'}
            </button>
            <Link className="btn btn--ghost" to="/links">
              <Icon name="list" size={16} />
              View my links
            </Link>
          </div>
        </form>

        {created && (
          <section className="result" aria-live="polite">
            <p className="result__label">Your short link is ready</p>
            <a
              className="result__link"
              href={created.shortLink}
              target="_blank"
              rel="noreferrer noopener"
            >
              {created.shortLink}
            </a>
            <p className="field__hint">Redirects to {created.target}</p>
            <div className="result__actions">
              <CopyButton value={created.shortLink} label="Copy link" />
              <a
                className="btn btn--secondary btn--small"
                href={created.shortLink}
                target="_blank"
                rel="noreferrer noopener"
              >
                <Icon name="external" size={16} />
                Open
              </a>
              <button type="button" className="btn btn--ghost btn--small" onClick={startOver}>
                <Icon name="plus" size={16} />
                Create another
              </button>
            </div>
          </section>
        )}
      </div>
    </>
  );
}
