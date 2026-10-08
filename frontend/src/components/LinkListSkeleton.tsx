/** Placeholder cards shown while the link list is loading. */
export function LinkListSkeleton({ count = 4 }: { count?: number }) {
  return (
    <ul className="link-grid" aria-hidden="true">
      {Array.from({ length: count }, (_, index) => (
        <li className="link-card" key={index}>
          <div className="skeleton" style={{ height: '1.1rem', width: '65%' }} />
          <div className="skeleton" style={{ height: '0.85rem', width: '90%' }} />
          <div className="skeleton" style={{ height: '0.75rem', width: '40%' }} />
          <div className="skeleton" style={{ height: '2rem', width: '55%' }} />
        </li>
      ))}
    </ul>
  );
}
