import { createUseStyles } from 'react-jss';

import CatalogDetailsPage from '../stores/catalogDetailsPage';
import Currency from './currency';
import BuyButton from './buyButton';

const useStyles = createUseStyles({
    itemDetails: {
        width: '100%',
        float: 'right',
    },

    detailContainer: {
        marginBottom: '12px',
        overflow: 'hidden',
    },

    priceAndBuyContainer: {
        marginBottom: '12px',
    },

    fieldContainer: {
        marginBottom: '12px',
        width: 'calc(100% - 190px)',
        float: 'left',
    },

    fieldLabel: {
        fontSize: '16px',
        fontWeight: '500',
        lineHeight: '1.4em',
        display: 'block',
        width: '120px',
        paddingRight: '9px',
        float: 'left',
        color: '#b8b8b8',
    },

    offSaleText: {
        paddingBottom: '12px',
    },
});

const ItemDetails = (props) => {
    const s = useStyles();
    const store = CatalogDetailsPage.useContainer();

    const details = store?.details || props?.details || null;

    if (!details) {
        return null;
    }

    const isResellAsset = Boolean(store?.isResellable);

    const price =
        details?.price && typeof details.price === 'object'
            ? details.price
            : null;

    const priceTickets =
        details?.priceTickets ??
        price?.priceTickets ??
        0;

    const priceRobux =
        details?.priceRobux ??
        price?.priceRobux ??
        0;

    const hasTicketsPrice =
        priceTickets !== null &&
        priceTickets !== undefined &&
        Number(priceTickets) > 0;

    const hasRobuxPrice =
        priceRobux !== null &&
        priceRobux !== undefined &&
        Number(priceRobux) > 0;

    const showBuyButton = (() => {
        if (isResellAsset) {
            return true;
        }

        if (details?.price === null) {
            return false;
        }

        return hasRobuxPrice || hasTicketsPrice;
    })();

    const showBuyTicketsButton =
        hasTicketsPrice && !isResellAsset;

    const showOrTab =
        !isResellAsset &&
        showBuyButton &&
        showBuyTicketsButton;

    const hasOffsaleLabel =
        store?.offsaleDeadline !== null &&
        store?.offsaleDeadline !== undefined &&
        !isResellAsset &&
        !showBuyButton;

    const buyButtonClick = (e) => {
        e.preventDefault();
        console.log('PRINTING!!');
    };

    return (
        <div className={s.itemDetails}>
            <div className={s.detailContainer}>
                {!hasOffsaleLabel ? (
                    <>
                        <div className={s.fieldContainer}>
                            <span className={s.fieldLabel}>
                                Price
                            </span>

                            <Currency
                                isRobux={!showBuyTicketsButton}
                            />
                        </div>

                        <BuyButton
                            isRobux={!showBuyTicketsButton}
                            onClick={buyButtonClick}
                            disabled={!showBuyButton}
                        />
                    </>
                ) : (
                    <div className={s.offSaleText}>
                        This item is not currently for sale.
                    </div>
                )}
            </div>

            {showOrTab && (
                <div className={s.detailContainer}>
                    <div className={s.fieldContainer}>
                        <span className={s.fieldLabel}>
                            Tickets
                        </span>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ItemDetails;