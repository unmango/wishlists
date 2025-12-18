package api

import (
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

func Path() chioas.Path {
	return chioas.Path{
		Paths: api.Paths,
	}
}

var api = chioas.Definition{
	Paths: chioas.Paths{
		"/wishlists": wishlists.Path(),
	},
}
