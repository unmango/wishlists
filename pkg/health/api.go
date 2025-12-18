package health

import (
	"net/http"

	"github.com/go-andiamo/chioas"
)

func Path() chioas.Path {
	return chioas.Path{
		Methods: chioas.Methods{
			http.MethodGet: {
				Handler: Handler,
			},
		},
	}
}
