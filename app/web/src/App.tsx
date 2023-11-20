import { useQuery } from "@apollo/client";
import { gql } from "./__generated__";
import "./App.css";

const LIST_WISHLISTS = gql(`
query ListWishlists {
	listWishlists {
		items
	}
}`);

function App() {
	const { data, loading, error, client } = useQuery(LIST_WISHLISTS);

	console.log(client);
	console.log(import.meta.env.VITE_API_URL);

	if (error) return <span>{JSON.stringify(error)}</span>;
	if (loading) return <span>Loading...</span>;

	return (
		<div>
			<span>Hiya</span>
			{!data && <span>Aww...</span>}
			{data?.listWishlists.map((l, i) => (
				<li key={i}>{l.items.join(",")}</li>
			))}
		</div>
	);
}

export default App;
