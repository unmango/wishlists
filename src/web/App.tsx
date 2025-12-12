import { Pane } from './components';

function App() {
  return (
    <div className='w-svw h-svh p-8 flex gap-4 bg-linear-to-r from-violet-600 to-indigo-800'>
      <Pane className='w-16 h-full p-4'>
			</Pane>
			<Pane className='w-full h-full p-4'>
        <h1>Oh hi</h1>
      </Pane>
    </div>
  );
}

export default App;
