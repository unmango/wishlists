// @ts-check

import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import react from 'eslint-plugin-react/configs/recommended';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';

export default tseslint.config(
  eslint.configs.recommended,
  ...tseslint.configs.strictTypeChecked,
	...tseslint.configs.stylisticTypeChecked,
	react,
	reactHooks,
	reactRefresh,
	{
		ignores: [
			'dist',
			'__generated__',
		],
	},
	{
		languageOptions: {
			parserOptions: {
				tsconfigRootDir: import.meta.dirname,
				project: [
					'./tsconfig.json',
					'./tsconfig.node.json',
				],
				sourceType: 'module',
				ecmaVersion: 'latest',
				ecmaFeatures: {
					jsx: true,
				},
			},
			globals: {
				...globals.browser,
			}
		},
		rules: {
			'react-refresh/only-export-components',
		},
	}
);
