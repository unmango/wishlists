package wishlists

import (
	"fmt"
	"io"
	"net/http"

	"github.com/charmbracelet/log"
	"github.com/go-andiamo/chioas"
	"github.com/nats-io/nats.go"
	"github.com/spf13/viper"
)

func Path() chioas.Path {
	return chioas.Path{
		Methods: chioas.Methods{
			http.MethodGet: {
				Handler: List,
			},
			http.MethodPost: {
				Handler: Create,
			},
		},
		Paths: chioas.Paths{
			"/{wishlistId}": {
				Methods: chioas.Methods{
					http.MethodGet: {
						Handler: Get,
					},
				},
			},
		},
	}
}

func Create(w http.ResponseWriter, req *http.Request) {
	if err := create(req); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
	} else {
		w.WriteHeader(http.StatusCreated)
	}
}

func create(req *http.Request) error {
	log.Info("Getting a NATS connection")
	conn, err := nats.Connect(viper.GetString("nats"))
	if err != nil {
		return err
	}
	defer conn.Drain()

	// Until I figure auth out
	userId := req.Header.Get("X-User-Id")
	body, err := io.ReadAll(req.Body)
	if err != nil {
		return err
	}

	log.Info("Publishing a create message", "user", userId)
	return conn.Publish(
		fmt.Sprintf("%s.wishlists", userId),
		body,
	)
}

func Get(w http.ResponseWriter, req *http.Request) {
	conn, err := nats.Connect(viper.GetString("nats"))
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer conn.Drain()

	w.Write([]byte(conn.ConnectedServerName()))
}

func List(w http.ResponseWriter, req *http.Request) {
	conn, err := nats.Connect(viper.GetString("nats"))
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer conn.Close()

	w.Write([]byte(conn.ConnectedUrl()))
}
