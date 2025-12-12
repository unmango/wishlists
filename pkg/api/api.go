package api

import (
	"net/http"

	"github.com/go-andiamo/chioas"
	"github.com/go-chi/chi/v5"
	"github.com/unmango/wishlists/pkg/wishlists"
)

func Router() (chi.Router, error) {
	r := chi.NewRouter()
	if err := api.SetupRoutes(r, api); err != nil {
		return nil, err
	} else {
		return r, nil
	}
}

func SetupRoutes(router chi.Router) error {
	return api.SetupRoutes(router, api)
}

func AsYaml() ([]byte, error) {
	return api.AsYaml()
}

var api = chioas.Definition{
	Paths: chioas.Paths{
		"/wishlists": {
			Methods: chioas.Methods{
				http.MethodGet: {
					Handler: wishlists.List,
				},
				http.MethodPost: {
					Handler: wishlists.Create,
				},
			},
		},
	},
}
