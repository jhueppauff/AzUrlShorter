import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';

export function NotFoundPage() {
  return (
    <div className="centered">
      <div className="centered__card">
        <span className="state__icon">
          <Icon name="alert" size={24} />
        </span>
        <h1 className="page-header__title">Page not found</h1>
        <p className="centered__text">
          There is nothing at this address. The link may have moved or never existed.
        </p>
        <Link className="btn btn--primary" to="/">
          Back to create a short link
        </Link>
      </div>
    </div>
  );
}
