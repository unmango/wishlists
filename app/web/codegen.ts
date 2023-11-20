import { CodegenConfig } from '@graphql-codegen/cli';
import * as path from 'path';

const config: CodegenConfig = {
	schema: path.join(__dirname, '..', '..', 'api', 'schema.graphql'),
	documents: ['src/**/*.{ts,tsx}'],
	generates: {
		'./src/__generated__/': {
			preset: 'client',
			plugins: [],
			presetConfig: {
				gqlTagName: 'gql',
			}
		}
	},
};

export default config;
