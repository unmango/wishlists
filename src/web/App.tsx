import { useEffect } from 'react';
import { ErrorMessage, Loading, TabBar } from './components/ui';
import { href, useMe, useRoute } from './hooks';
import { Buying } from './screens/Buying';
import { Family } from './screens/Family';
import { MyList } from './screens/MyList';
import { SignIn } from './screens/SignIn';
import { TheirList } from './screens/TheirList';

function App() {
  const { me, error, isLoading } = useMe();
  const route = useRoute();

  // Your own list opened by link (someone shared it) shows the owner's view.
  const ownList = route.name === 'list' && me && route.id === me.wishlistId;
  useEffect(() => {
    if (ownList) window.location.replace(href({ name: 'mine' }));
  }, [ownList]);

  if (isLoading) return <Loading />;
  if (error) return <ErrorMessage error={error} />;
  if (!me) return <SignIn />;

  return (
    <>
      {route.name === 'family' && <Family me={me} />}
      {route.name === 'list' && !ownList && <TheirList key={route.id} id={route.id} me={me} />}
      {route.name === 'mine' && <MyList />}
      {route.name === 'buying' && <Buying />}
      <TabBar active={route.name} />
    </>
  );
}

export default App;
